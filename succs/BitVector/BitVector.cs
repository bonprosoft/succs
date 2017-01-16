using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SucCs.WaveletMatrix;

namespace SucCs.BitVector
{

    // Store the bit array, but it stores little endian form in block.

    /// <summary>
    /// Uncompressed BitVector, which provides so many operations in constant time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In implementation, we set <see cref="BLOCK_SIZE"/> to ulong (64bit), which is appropriate size for 64bit architecture.
    /// And for reducing space complexity, we won't store each rank of block, which is easily calculated in constant bit operation.
    /// <para>
    /// We set <see cref="RANK_TABLE_SIZE"/> to 256bit (stores 4 blocks in each table).
    /// </para>
    /// </para>
    /// <para>
    /// If you want to just attach index to existing array instance (e.g. bool[], ulong[]),
    /// you should use <see cref="BitIndex{T}"/> instead.
    /// </para>
    /// <para>
    /// Note that classes in <see cref="SucCs.BitVector"/> namespace process data as bit-array.
    /// If you want to deal with multiple values, use <see cref="WaveletMatrix"/> instead.
    /// </para>
    /// </remarks>
    public class BitVector : IBitVector
    {
        // each block contains ulong(=2**64 bits)
        public const int BLOCK_SIZE = 1 << 6;

        // each block-group contains 4 blocks, so max pop-count must be byte(=2**8 bits).
        // but it doesn't matter because we won't store pop-count of each block in this class.
        public const int RANK_TABLE_RATIO = 1 << 2;

        // max value of this table must be the length of BitVector (< ulong size)
        public const int RANK_TABLE_SIZE = BLOCK_SIZE * RANK_TABLE_RATIO;

        private List<ulong> _rankTable = new List<ulong>(0);
        private List<ulong> _bitTable = new List<ulong>(0);

        private ulong _length = 0;
        private ulong _totalPopCount = 0;

        internal void Initialize(ulong length)
        {
            try
            {
                checked
                {
                    var blockCount = (int)((length + BLOCK_SIZE - 1) / BLOCK_SIZE);
                    this._bitTable.Clear();
                    this._bitTable.Capacity = blockCount;
                    this._bitTable.AddRange(new ulong[blockCount]);

                    var tableCount = ((blockCount + RANK_TABLE_RATIO - 1) / RANK_TABLE_RATIO) + 1;
                    this._rankTable.Clear();
                    this._rankTable.Capacity = tableCount;
                    this._rankTable.AddRange(new ulong[tableCount]);

                    this._length = length;
                    this._totalPopCount = 0;
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Specified array length: ${length} is too long!");
            }
        }

        private ulong RankOne(ulong count)
        {
            var blockIndex = (int)(count / BLOCK_SIZE);
            var tableIndex = blockIndex / RANK_TABLE_RATIO;

            var rankOne = this._rankTable[tableIndex];
            for (var i = tableIndex * RANK_TABLE_RATIO; i < blockIndex; i++)
                rankOne += BitUtility.PopCountInt64(this._bitTable[i]);

            // max value
            if (blockIndex >= this._bitTable.Count)
                return rankOne;

            rankOne += BitUtility.RankInt64OfLowerMask(this._bitTable[blockIndex], (int)(count % BLOCK_SIZE), true);
            return rankOne;
        }

        public BitVector(long length) : this((ulong)length)
        {
        }

        public BitVector(ulong length)
        {
            this.Initialize(length);
        }

        public ulong Length => this._length;

        public ulong TotalPopCount() => this._totalPopCount;

        public void Set(ulong index, bool value)
        {
            if (index >= this._length)
                throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");
            if (value)
            {
                this._bitTable[(int)(index / BLOCK_SIZE)] |= (1UL << (int)(index % BLOCK_SIZE));
            }
            else
            {
                this._bitTable[(int)(index / BLOCK_SIZE)] &= ~(1UL << (int)(index % BLOCK_SIZE));
            }
        }

        public void SetRange(ulong index, bool[] value)
        {
            if (value == null || value.Length == 0)
                throw new InvalidOperationException($"Value must be not null and its length >= 1.");

            var maxIndex = index + (ulong)value.Length - 1UL;
            if (index >= this._length || maxIndex >= this._length)
                throw new IndexOutOfRangeException($"Operation to index:{index} to {maxIndex} is out of range.");


            var i = 0UL;

            var startBlock = (int)(index / BLOCK_SIZE);
            var endBlock = (int)(maxIndex / BLOCK_SIZE);

            for (var j = startBlock; j <= endBlock; j++)
            {
                var startMask = 0;
                var endMask = BLOCK_SIZE - 1;

                if (j == startBlock) startMask = (int)(index % BLOCK_SIZE);
                if (j == endBlock) endMask = (int)(maxIndex % BLOCK_SIZE);

                var tempMask = 1UL << startMask;
                for (var k = startMask; k <= endMask; k++)
                {
                    // TODO: Create range mask per each block for executing bit operation. (it enables us to set values within only 2 operations, | and &)
                    if (value[i])
                    {
                        this._bitTable[j] |= tempMask;
                    }
                    else
                    {
                        this._bitTable[j] &= ~tempMask;
                    }
                    i++;
                    tempMask = tempMask << 1;
                }
            }
        }

        public void Toggle(ulong index)
        {
            if (index >= this._length)
                throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");

            this._bitTable[(int)(index / BLOCK_SIZE)] ^= (1UL << (int)(index % BLOCK_SIZE));
        }

        public void ToggleRange(ulong offset, ulong count)
        {
            if (count == 0)
                throw new InvalidOperationException($"Count must be positive integer.");

            var maxIndex = offset + count - 1UL;
            if (maxIndex >= this._length)
                throw new IndexOutOfRangeException($"Operation to index:{offset} to {maxIndex} is out of range.");

            var startBlock = (int)(offset / BLOCK_SIZE);
            var endBlock = (int)(maxIndex / BLOCK_SIZE);

            for (var j = startBlock; j <= endBlock; j++)
            {
                var startMask = 0;
                var endMask = BLOCK_SIZE - 1;

                if (j == startBlock) startMask = (int)(offset % BLOCK_SIZE);
                if (j == endBlock) endMask = (int)(maxIndex % BLOCK_SIZE);

                var tempMask = 1UL << startMask;
                for (var k = startMask; k <= endMask; k++)
                {
                    this._bitTable[j] ^= tempMask;
                    tempMask = tempMask << 1;
                }
            }
        }

        public bool Access(ulong index)
        {
            if (index >= this._length)
                throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");

            return (((this._bitTable[(int)(index / BLOCK_SIZE)] >> (int)(index % BLOCK_SIZE)) & 1UL) != 0);
        }

        public IEnumerable<bool> AccessRange(ulong offset, ulong count)
        {
            if (count == 0)
                throw new InvalidOperationException($"Count must be positive integer.");

            var lastIndex = offset + count;

            if (lastIndex >= this._length)
                throw new IndexOutOfRangeException($"Operation from:{offset} to {lastIndex} is out of range.");

            var startBlock = (int)(offset / BLOCK_SIZE);
            var endBlock = (int)(lastIndex / BLOCK_SIZE);

            for (var i = startBlock; i <= endBlock; i++)
            {
                var startMask = 0;
                var endMask = BLOCK_SIZE - 1;

                if (i == startBlock) startMask = (int)(offset % BLOCK_SIZE);
                if (i == endBlock) endMask = (int)(lastIndex % BLOCK_SIZE);

                var value = this._bitTable[i] >> startMask;
                for (var j = startMask; j <= endMask; j++)
                {
                    yield return ((value & 1UL) != 0);
                    value = value >> 1;
                }
            }
        }

        public ulong Rank(bool value, ulong count)
        {
            if (count == 0)
                throw new InvalidOperationException($"Count must be positive integer.");

            if (count > this._length)
                throw new IndexOutOfRangeException($"Operation to index:{count} is out of range.");

            return value ? RankOne(count) : (count - RankOne(count));
        }

        public ulong Rank(bool value, ulong count, ulong offset)
        {
            if (count == 0)
                throw new InvalidOperationException($"Count must be positive integer.");

            var index = offset + count;
            if (index > this._length)
                throw new IndexOutOfRangeException($"Operation from offset:{offset} to index:{index} is out of range.");

            ulong result = 0;
            if (offset == 0)
            {
                result = RankOne(index);
            }
            else
            {
                result = RankOne(index) - RankOne(offset);
            }

            return value ? result : ((index - offset) - result);
        }

        public ulong Select(bool value, ulong rank)
        {
            if (rank == 0)
                throw new InvalidOperationException($"Rank must be positive integer.");

            if ((value && rank > this._totalPopCount) ||
                (!value && rank > (this._length - this._totalPopCount)))
                throw new InvalidOperationException($"Count:{rank} is larger than total pop count.");

            // Search block using binary search
            var left = 0;
            var right = this._rankTable.Count;
            while (left < right)
            {
                var mid = (left + right) / 2;
                var midRank = this._rankTable[mid];
                if (!value) midRank = ((ulong)mid * RANK_TABLE_SIZE) - midRank;
                if (midRank < rank)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid;
                }
            }

            var tableIndex = (left != 0) ? left - 1 : 0;
            var blockIndex = RANK_TABLE_RATIO * tableIndex;

            if (value)
            {
                rank -= this._rankTable[tableIndex];
            }
            else
            {
                rank -= ((ulong)tableIndex * RANK_TABLE_SIZE) - this._rankTable[tableIndex];
            }

            // Search block (no index in this implementation)
            for (; blockIndex < this._bitTable.Count; blockIndex++)
            {
                var blockRank = BitUtility.RankInt64(this._bitTable[blockIndex], value);
                if (rank <= blockRank)
                    break; // this block contains enough popcount.

                rank -= blockRank;
            }

            // Search within block using bit operation.
            return ((ulong)blockIndex * BLOCK_SIZE) +
                   BitUtility.SelectInt64(this._bitTable[blockIndex], value, (uint)rank);
        }

        public ulong Select(bool value, ulong rank, ulong offset)
        {
            if (rank == 0)
                throw new InvalidOperationException($"Rank must be positive integer.");

            if (offset + rank > this._length)
                throw new IndexOutOfRangeException($"Operation: rank={rank} from offset={offset} is out of range.");

            if ((value && rank > this._totalPopCount) ||
                (!value && rank > (this._length - this._totalPopCount)))
                throw new InvalidOperationException($"Count:{rank} is larger than total pop count.");

            if (offset == 0)
            {
                return Select(value, rank);
            }
            else
            {
                var totalRank = Rank(value, offset) + rank;
                if ((value && totalRank > this._totalPopCount) ||
                    (!value && totalRank > (this._length - this._totalPopCount)))
                    throw new InvalidOperationException(
                        $"Count:{rank} is larger than total pop count in BitVector (offset:{offset}).");

                return Select(value, totalRank);
            }
        }

        public void Clear()
        {
            this.Initialize(this._length);
        }

        public void Resize(ulong length)
        {
            this.Initialize(length);
        }

        public void Extend(ulong count)
        {
            var length = this._length + count;

            try
            {
                checked
                {
                    var blockCount = (int)((length + BLOCK_SIZE - 1) / BLOCK_SIZE);
                    this._bitTable.Capacity = blockCount;
                    var needToAdd = blockCount - this._bitTable.Count;
                    if (needToAdd > 0)
                        this._bitTable.AddRange(new ulong[needToAdd]);

                    this._length = length;
                    this._totalPopCount = 0;

                    BuildIndex();
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Cannot extend the array: Estimated array length: ${length} is too long!");
            }
        }

        public void BuildIndex()
        {
            try
            {
                checked
                {
                    var tableCount = (int)((this._bitTable.Count + RANK_TABLE_RATIO - 1) / RANK_TABLE_RATIO) + 1;
                    this._rankTable.Clear();
                    this._rankTable.Capacity = tableCount;
                    this._rankTable.AddRange(new ulong[tableCount]);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Array length is too long!");
            }

            var blockCount = this._bitTable.Count;
            var total = 0UL;

            for (var i = 0; i < this._rankTable.Count; i++)
            {
                this._rankTable[i] = total;

                for (var j = (i * RANK_TABLE_RATIO); (j < blockCount) && (j < ((i + 1) * RANK_TABLE_RATIO)); j++)
                    total += BitUtility.PopCountInt64(this._bitTable[j]);
            }
            this._totalPopCount = total;
        }

        public bool this[ulong index]
        {
            get { return this.Access(index); }
            set { this.Set(index, value); }
        }

        public void Dispose()
        {
            this._bitTable?.Clear();
            this._bitTable = null;
            this._rankTable?.Clear();
            this._rankTable = null;
            GC.SuppressFinalize(this);
        }
    }
}
