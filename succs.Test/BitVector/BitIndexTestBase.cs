using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SucCs.BitVector;
using SucCs.BitVector.Mapper;
using Xunit;

namespace SucCs.Test.BitVector
{
    public abstract class BitIndexTestBase<T> where T : IIndexAccessibleCollection
    {

        protected List<bool> _items = null;

        protected SucCs.BitVector.UShortBitIndex<T> _largeBi = null;

        protected SucCs.BitVector.BitIndex<T> _bi = null;

        protected BitIndexTestBase()
        {
            Initialize();
        }

        protected void Initialize()
        {
            this._items = new List<bool>();
            var r = new Random();

            for (var i = 0; i < TestCondition.TEST_CASE_BITS; i++)
                this._items.Add(r.Next(0, 2) == 0);

            InitializeBitIndexInstance();

            this._bi.BuildIndex();
            this._largeBi.BuildIndex();
        }

        protected abstract void InitializeBitIndexInstance();

        [Fact]
        public void RankTest()
        {
            this.RankTestImpl(this._bi);
        }

        [Fact]
        public void RankUShortTest()
        {
            this.RankTestImpl(this._largeBi);
        }

        private void RankTestImpl(IBitIndex bi)
        {
            var totalRankOne = bi.Rank(true, (ulong)this._items.Count);
            var totalRankZero = bi.Rank(false, (ulong)this._items.Count);

            Assert.Equal<ulong>((ulong)this._items.Count, totalRankOne + totalRankZero);
            var oneCount = (ulong)this._items.LongCount(c => c);
            Assert.Equal<ulong>(oneCount, totalRankOne);

            oneCount = 0;
            for (var i = 1UL; i <= TestCondition.TEST_CASE_BITS; i++)
            {
                oneCount += (this._items[(int)(i - 1)] ? 1UL : 0UL);
                Assert.Equal<ulong>(oneCount, bi.Rank(true, i));
                Assert.Equal<ulong>(i - oneCount, bi.Rank(false, i));
            }

            Assert.Equal<ulong>(oneCount, bi.TotalPopCount());

            Assert.Throws<IndexOutOfRangeException>(() => { bi.Rank(true, (ulong)bi.Length + 1); });

            var random = new Random();

            for (var i = 0; i < TestCondition.SAMPLE_QUERY_CASE; i++)
            {
                var start = random.Next(0, this._items.Count / 2);
                var count = random.Next(1, this._items.Count - start + 1);

                oneCount = (ulong)this._items.Skip(start).Take(count).LongCount(c => c);
                Assert.Equal<ulong>(oneCount, bi.Rank(true, (ulong)count, (ulong)start));
                Assert.Equal<ulong>((ulong)count - oneCount, bi.Rank(false, (ulong)count, (ulong)start));
            }

            Assert.Throws<InvalidOperationException>(() => { bi.Rank(true, 0); });
            Assert.Throws<IndexOutOfRangeException>(() => { bi.Rank(true, (ulong)bi.Length, 5); });
            Assert.Throws<InvalidOperationException>(() => { bi.Rank(true, 0, 5); });
        }

        [Fact]
        public void AccessTest()
        {
            this.AccessImpl(this._bi);
        }

        [Fact]
        public void AccessUShortTest()
        {
            this.AccessImpl(this._largeBi);
        }

        private void AccessImpl(IBitIndex bi)
        {
            for (var i = 0; i < this._items.Count; i++)
                Assert.Equal<bool>(this._items[i], bi.Access((ulong)i));

            Assert.Equal<bool>(this._items[10], bi[10]);

            var temp = 10;
            foreach (var item in bi.AccessRange(10, 10))
            {
                Assert.Equal<bool>(this._items[temp], item);
                temp++;
            }
        }

        [Fact]
        public void SelectTest()
        {
            this.SelectImpl(this._bi);
        }

        [Fact]
        public void SelectUShortTest()
        {
            this.SelectImpl(this._largeBi);
        }

        private void SelectImpl(IBitIndex bi)
        {
            var oneCount = 0UL;
            var zeroCount = 0UL;

            for (var i = 0; i < TestCondition.TEST_CASE_BITS; i++)
            {
                if (this._items[i])
                {
                    oneCount++;
                    Assert.Equal<ulong>((ulong)i, bi.Select(true, oneCount));
                }
                else
                {
                    zeroCount++;
                    Assert.Equal<ulong>((ulong)i, bi.Select(false, zeroCount));
                }
            }

            // in some mapper (UIntListMapper, ULongListMapper), it takes enough memory for containing all data.
            var actualZeroCount = bi.Rank(false, bi.Length);

            Assert.Throws<InvalidOperationException>(() => { bi.Select(true, oneCount + 1); });
            Assert.Throws<InvalidOperationException>(() => { bi.Select(false, actualZeroCount + 1); });

            var random = new Random();

            for (var i = 0; i < TestCondition.SAMPLE_QUERY_CASE; i++)
            {
                var start = random.Next(0, this._items.Count / 2);
                var rank = random.Next(1, 100);

                var indexOne = (ulong)this._items.Select((c, ind) => new Tuple<int, bool>(ind, c))
                                                 .Skip(start)
                                                 .Where(c => c.Item2)
                                                 .Skip(rank - 1)
                                                 .First().Item1;

                var indexZero = (ulong)this._items.Select((c, ind) => new Tuple<int, bool>(ind, c))
                                                  .Skip(start)
                                                  .Where(c => !c.Item2)
                                                  .Skip(rank - 1)
                                                  .First().Item1;

                Assert.Equal<ulong>(indexOne, bi.Select(true, (ulong)rank, (ulong)start));
                Assert.Equal<ulong>(indexZero, bi.Select(false, (ulong)rank, (ulong)start));
            }

            Assert.Throws<InvalidOperationException>(() => { bi.Select(true, 0); });
            Assert.Throws<InvalidOperationException>(() => { bi.Select(false, 0, 10); });
            Assert.Throws<IndexOutOfRangeException>(() => { bi.Select(true, 10, bi.Length); });
        }

    }
}
