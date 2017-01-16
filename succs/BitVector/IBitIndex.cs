using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SucCs.BitVector
{
    public interface IBitIndex : IDisposable
    {
        ulong Length { get; }

        ulong TotalPopCount();

        bool Access(ulong index);
        IEnumerable<bool> AccessRange(ulong offset, ulong count);

        ulong Rank(bool value, ulong count);
        ulong Rank(bool value, ulong count, ulong offset);

        ulong Select(bool value, ulong rank);
        ulong Select(bool value, ulong rank, ulong offset);

        void BuildIndex();

        bool this[ulong index] { get; }

    }
}
