using System;
using System.Collections.Generic;
using SucCs.BitVector;

namespace SucCs.WaveletMatrix
{
    public class WaveletMatrix<TSource, TBitVector> : IWaveletMatrix
        where TSource : IIndexAccessibleCollection
        where TBitVector : IBitVector
    {
        private IIndexAccessibleCollection _source;

        public WaveletMatrix(TSource source, int alphabetSize, bool buildIndex = false)
        {
            this._source = source;
            if (buildIndex)
                BuildIndex();
        }

        public ulong Length { get; }

        public int Access(ulong index)
        {
            throw new NotImplementedException();
        }

        public ulong Rank(int value, ulong count)
        {
            throw new NotImplementedException();
        }

        public ulong Rank(int value, ulong count, ulong offset)
        {
            throw new NotImplementedException();
        }

        public ulong Select(int value, ulong rank)
        {
            throw new NotImplementedException();
        }

        public ulong Select(int value, ulong rank, ulong offset)
        {
            throw new NotImplementedException();
        }

        public int Quantie(ulong offset, ulong count, int r)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> TopK(ulong offset, ulong count, int k)
        {
            throw new NotImplementedException();
        }

        public ulong RangeFreq(ulong offset, ulong count, int min, int max)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<int, ulong>> RangeList(ulong offset, ulong count, int min, int max)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> RangeMaxK(ulong offset, ulong count, int k)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> RangeMinK(ulong offset, ulong count, int k)
        {
            throw new NotImplementedException();
        }

        public int PrevValue(ulong offset, ulong count, ulong x, ulong y)
        {
            throw new NotImplementedException();
        }

        public int NextValue(ulong offset, ulong count, ulong x, ulong y)
        {
            throw new NotImplementedException();
        }

        public int Intersect(ulong offset, ulong count, ulong u, ulong v)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Resize(ulong length)
        {
            throw new NotImplementedException();
        }

        public int this[ulong index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void BuildIndex()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this._source = null;
        }
    }
}
