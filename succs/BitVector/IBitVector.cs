using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SucCs.BitVector
{
    public interface IBitVector : IBitIndex
    {
        void Set(ulong index, bool value);
        void SetRange(ulong index, bool[] value);
        void Toggle(ulong index);
        void ToggleRange(ulong offset, ulong count);

        void Clear();

        void Resize(ulong length);
        void Extend(ulong count);

        new bool this[ulong index] { get; set; }
    }
}
