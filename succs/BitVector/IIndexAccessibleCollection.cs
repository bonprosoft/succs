using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SucCs.BitVector
{
    public interface IIndexAccessibleCollection : IDisposable
    {
        ulong AvailableLength { get; }

        ulong GetRangeInt64(ulong index);

        bool Access(ulong index);
        IEnumerable<bool> AccessRange(ulong offset, ulong count);

        bool this[ulong index] { get; }
    }
}
