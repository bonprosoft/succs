using System;
using System.Collections.Generic;
using System.Text;
using SucCs.BitVector;

namespace SucCs.WaveletMatrix
{
    public interface IWaveletMatrix : IDisposable
    {
        ulong Length { get; }

        int Access(ulong index);

        ulong Rank(int value, ulong count);
        ulong Rank(int value, ulong count, ulong offset);

        ulong Select(int value, ulong rank);
        ulong Select(int value, ulong rank, ulong offset);

        int Quantie(ulong offset, ulong count, int r);

        IEnumerable<int> TopK(ulong offset, ulong count, int k);

        ulong RangeFreq(ulong offset, ulong count, int min, int max);

        IEnumerable<Tuple<int, ulong>> RangeList(ulong offset, ulong count, int min, int max);

        IEnumerable<int> RangeMaxK(ulong offset, ulong count, int k);

        IEnumerable<int> RangeMinK(ulong offset, ulong count, int k);

        int PrevValue(ulong offset, ulong count, ulong x, ulong y);

        int NextValue(ulong offset, ulong count, ulong x, ulong y);

        int Intersect(ulong offset, ulong count, ulong u, ulong v);

        void Clear();

        void Resize(ulong length);

        int this[ulong index] { get; set; }

        void BuildIndex();

    }
}
