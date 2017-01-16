using System;
using System.Collections.Generic;
using System.Runtime;

namespace SucCs.BitVector.Mapper
{
    public class BoolListMapper : IIndexAccessibleCollection
    {
        public BoolListMapper(IList<bool> data)
        {
            this.Data = data;
        }

        public IList<bool> Data { get; private set; }

        public ulong AvailableLength => (ulong)this.Data.Count;

        public ulong GetRangeInt64(ulong index)
        {
            return BitUtility.BoolToUlong(this.Data, (int)index);
        }

        public bool Access(ulong index)
        {
            try
            {
                checked
                {
                    var intIndex = (int)index;
                    if (intIndex >= this.Data.Count)
                        throw new IndexOutOfRangeException($"Operation to index:{index} is out of range.");

                    return this.Data[intIndex];
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {index}!\nYou should use another multiple-value array, e.g. BitVector, List<ulong>.");
            }
        }

        public IEnumerable<bool> AccessRange(ulong offset, ulong count)
        {
            var startIndex = 0;
            var maxIndex = 0;

            try
            {
                checked
                {
                    startIndex = (int)offset;
                    maxIndex = (int)(offset + count);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"Overflow exception for index {offset} to {offset + count} !\nYou should use another multiple-value array, e.g. BitVector, List<ulong>.");
            }

            for (var i = startIndex; i < this.Data.Count && i < maxIndex; i++)
            {
                yield return this.Data[i];
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
