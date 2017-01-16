using System;
using System.Collections.Generic;

namespace SucCs.BitVector.Mapper
{
    public class UIntListMapper : IIndexAccessibleCollection
    {
        public UIntListMapper(IList<uint> data)
        {
            this.Data = data;
        }

        public IList<uint> Data { get; private set; }
        public ulong AvailableLength => (ulong)this.Data.Count * 32;

        public ulong GetRangeInt64(ulong index)
        {
            if (index % 32 != 0)
                throw new InvalidOperationException($"index: {index} must be multiple of block size: 32");

            var startIndex = (int)(index / 32);
            return BitUtility.UIntToUlong(this.Data, startIndex);
        }

        public bool Access(ulong index)
        {
            try
            {
                checked
                {
                    var intIndex = (int)(index / 32);

                    if (intIndex >= this.Data.Count)
                        throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");

                    var rest = (int)(index % 32);
                    return (((this.Data[intIndex] >> (31 - rest)) & 1UL) != 0);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {index}!\nYou should use another multiple-value array, e.g. BitVector, List<ulong>.");
            }
        }

        public IEnumerable<bool> AccessRange(ulong offset, ulong count)
        {
            var startBlock = 0;
            var endBlock = 0;

            try
            {
                checked
                {
                    startBlock = (int)(offset / 32);
                    endBlock = (int)((offset + count) / 32);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {offset} to {offset + count} !\nYou should use another multiple-value array, e.g. BitVector, List<ulong>.");
            }

            for (var i = startBlock; i < this.Data.Count && i < endBlock; i++)
            {
                var startMask = 0;
                var endMask = 31;

                if (i == startBlock) startMask = (int)(offset % 32);
                if (i == endBlock) endMask = (int)((offset + count) % 32);

                var value = BitUtility.ReverseInt32(this.Data[i]) << startMask;
                for (var j = startMask; j <= endMask; j++)
                {
                    yield return ((value & 1UL) != 0);
                    value = value >> 1;
                }
            }
        }

        public bool this[ulong index] => this.Access(index);
        public void Dispose()
        {
            this.Data = null;
            GC.SuppressFinalize(this);
        }
    }
}
