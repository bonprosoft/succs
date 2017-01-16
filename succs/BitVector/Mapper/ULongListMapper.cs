using System;
using System.Collections.Generic;

namespace SucCs.BitVector.Mapper
{
    public class ULongListMapper : IIndexAccessibleCollection
    {
        public ULongListMapper(IList<ulong> data)
        {
            this.Data = data;
        }

        public IList<ulong> Data { get; private set; }
        public ulong AvailableLength => (ulong)this.Data.Count * 64;

        public ulong GetRangeInt64(ulong index)
        {
            if (index % 64 != 0)
                throw new InvalidOperationException($"index: {index} must be multiple of block size: 64");

            return this.Data[(int)(index / 64)];
        }

        public bool Access(ulong index)
        {
            try
            {
                checked
                {
                    var intIndex = (int)(index / 64);

                    if (intIndex >= this.Data.Count)
                        throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");

                    var rest = (int)(index % 64);
                    return (((this.Data[intIndex] >> (63 - rest)) & 1UL) != 0);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {index}!\nYou should use another multiple-value array, e.g. BitVector");
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
                    startBlock = (int)(offset / 64);
                    endBlock = (int)((offset + count) / 64);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {offset} to {offset + count} !\nYou should use another multiple-value array, e.g. BitVector.");
            }

            for (var i = startBlock; i < this.Data.Count && i < endBlock; i++)
            {
                var startMask = 0;
                var endMask = 63;

                if (i == startBlock) startMask = (int)(offset % 64);
                if (i == endBlock) endMask = (int)((offset + count) % 64);

                var value = BitUtility.ReverseInt64(this.Data[i]) << startMask;
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
