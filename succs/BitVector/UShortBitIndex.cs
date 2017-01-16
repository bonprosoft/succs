using System;
using System.Collections.Generic;
using System.Text;

namespace SucCs.BitVector
{
    /// <summary>
    /// Uncompressed index like BitVector has. This class provides so many operations in constant time.
    /// No operation is provided for changing the value of data. (for supporting <see cref="IReadOnlyList{T}"/>, etc.)
    /// <para>
    /// If you deal with large data (n >> ushort), this class provides more effecient operations than <see cref="BitIndex{T}"/>.
    /// In other cases, you should use <see cref="BitIndex{T}"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that no operation is provided for changing the value of data in this class. (for supporting IReadOnlyCollection, etc.)
    /// If you want to change it, you should do it using the data instance, then call <see cref="BuildIndex()"/> method.
    /// </para>
    /// <para>
    /// In implementation, we set <see cref="BLOCK_SIZE"/> to ulong (64bit), which is appropriate size for 64bit architecture.
    /// In case of taking more than constant time to access specified bits,
    ///  we will store each rank of block unlike that of <see cref="SucCs.BitVector.BitVector"/> implementation. (for reducing data access)
    /// </para>
    /// <para>
    /// We set <see cref="RANK_TABLE_SIZE"/> to 256bit (stores 4 blocks in each table),
    ///  but if you want to deal with more large data, you should use <see cref="UShortBitIndex{T}"/> instead.
    /// </para>
    /// <para>
    /// Note that classes in <see cref="SucCs.BitVector"/> namespace process data as bit-array.
    /// If you want to deal with multiple values, use <see cref="WaveletMatrix"/> instead.
    /// </para>
    /// </remarks>
    public class UShortBitIndex<T> : IBitIndex
         where T : IIndexAccessibleCollection
    {
        // TODO: define micro to change 32bit, 64bit (using alias)

        // each block contains ulong(=2**64 bits)
        public const int BLOCK_SIZE = 1 << 6;

        // each block-group contains 4 blocks, so max pop-count must be byte(=2**8 bits).
        // but it **does** matter because we will store pop-count of each block in this class.
        public const int RANK_TABLE_RATIO = 1 << 10;

        // max value of this table must be the length of BitVector (< ulong size)
        public const int RANK_TABLE_SIZE = BLOCK_SIZE * RANK_TABLE_RATIO;

        private IIndexAccessibleCollection _data;

        private List<ulong> _rankTable = new List<ulong>(0);
        private List<ushort> _blockTable = new List<ushort>(0);

        private ulong _length = 0;
        private ulong _totalPopCount = 0;

        public UShortBitIndex(IIndexAccessibleCollection data, bool buildIndex = false)
        {
            _data = data;
            if (buildIndex)
                BuildIndex();
        }

        private ulong RankOne(ulong count)
        {
            var blockIndex = (int)(count / BLOCK_SIZE);
            var tableIndex = blockIndex / RANK_TABLE_RATIO;
            var rest = (int)(count % BLOCK_SIZE);

            var rankOne = this._rankTable[tableIndex] + this._blockTable[blockIndex];

            if (rest > 0)
                rankOne += BitUtility.RankInt64(this._data.GetRangeInt64((ulong)blockIndex * BLOCK_SIZE), rest, true);

            return rankOne;
        }

        public ulong Length => this._length;

        public ulong TotalPopCount() => this._totalPopCount;
        public bool Access(ulong index)
        {
            return this._data.Access(index);
        }

        public IEnumerable<bool> AccessRange(ulong offset, ulong count)
        {
            return this._data.AccessRange(offset, count);
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
            var baseRank = rank;
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
            var nextBlockStart = RANK_TABLE_RATIO * (tableIndex + 1);

            if (value)
            {
                rank -= this._rankTable[tableIndex];
            }
            else
            {
                rank -= ((ulong)tableIndex * RANK_TABLE_SIZE) - this._rankTable[tableIndex];
            }


            // Search block (in this implementation, we **have** rank value of each block already.)
            // in this case, each table have just 2<<10 blocks, so we use binary search.
            var min = (nextBlockStart > this._blockTable.Count) ? this._blockTable.Count : nextBlockStart;
            var baseIndex = blockIndex;
            left = blockIndex;
            right = min;
            while (left < right)
            {
                var mid = (left + right) / 2;
                var midRank = this._blockTable[mid];
                if (!value) midRank = (ushort)(((mid - baseIndex) * BLOCK_SIZE) - midRank);
                if (midRank < rank)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid;
                }
            }

            blockIndex = (left != 0) ? left - 1 : 0;

            if (value)
            {
                rank -= this._blockTable[blockIndex];
            }
            else
            {
                rank -= ((ulong)(blockIndex - baseIndex) * BLOCK_SIZE) - this._blockTable[blockIndex];
            }

            // Search within block using bit operation.
            var startIndex = (ulong)blockIndex * BLOCK_SIZE;
            return (startIndex + BitUtility.SelectInt64(BitUtility.ReverseInt64(this._data.GetRangeInt64(startIndex)), value, (uint)rank));
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

        public void BuildIndex()
        {
            var blockCount = 0;

            try
            {
                checked
                {
                    var length = this._data.AvailableLength;

                    blockCount = (int)((length + BLOCK_SIZE - 1) / BLOCK_SIZE) + 1;
                    this._blockTable.Clear();
                    this._blockTable.Capacity = blockCount;
                    this._blockTable.AddRange(new ushort[blockCount]);

                    var tableCount = ((blockCount + RANK_TABLE_RATIO - 1) / RANK_TABLE_RATIO);
                    this._rankTable.Clear();
                    this._rankTable.Capacity = tableCount;
                    this._rankTable.AddRange(new ulong[tableCount]);

                    this._length = length;
                    this._totalPopCount = 0;
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Array length is too long!");
            }

            var total = 0UL;

            try
            {
                checked
                {
                    for (var i = 0; i < this._rankTable.Count; i++)
                    {
                        this._rankTable[i] = total;
                        ushort subTotal = 0;

                        var j = i * RANK_TABLE_RATIO;
                        this._blockTable[j] = 0;

                        for (; (j < blockCount - 1) && (j < ((i + 1) * RANK_TABLE_RATIO));)
                        {
                            subTotal += (ushort)BitUtility.PopCountInt64(this._data.GetRangeInt64((ulong)j * BLOCK_SIZE));
                            this._blockTable[++j] = subTotal;
                        }

                        total += subTotal;
                    }
                    this._totalPopCount = total;
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"[Bug] Popcount must be in range of byte. (ref: TABLE_RATIO={RANK_TABLE_SIZE}");
            }
        }

        public void Dispose()
        {
            // won't dispose data instance (just release referene)
            this._data = null;

            this._blockTable?.Clear();
            this._blockTable = null;
            this._rankTable?.Clear();
            this._rankTable = null;
            GC.SuppressFinalize(this);
        }

        public bool this[ulong index] => this.Access(index);
    }
}
