using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using SucCs;

namespace SucCs.Test.BitVector
{
    public class BitVectorTest
    {
        private List<bool> _items = null;

        private SucCs.BitVector.BitVector _bv = null;

        public BitVectorTest()
        {
            Initialize();
        }

        private void Initialize()
        {
            this._items = new List<bool>();
            var r = new Random();

            for (var i = 0; i < TestCondition.TEST_CASE_BITS; i++)
                this._items.Add(r.Next(0, 2) == 0);

            this._bv = new SucCs.BitVector.BitVector(this._items.Count);
            this._bv.SetRange(0, this._items.ToArray());
            this._bv.BuildIndex();
        }

        [Fact]
        public void RankTest()
        {
            var totalRankOne = this._bv.Rank(true, (ulong)this._items.Count);
            var totalRankZero = this._bv.Rank(false, (ulong)this._items.Count);

            Assert.Equal<ulong>((ulong)this._items.Count, totalRankOne + totalRankZero);
            var oneCount = (ulong)this._items.LongCount(c => c);
            Assert.Equal<ulong>(oneCount, totalRankOne);

            oneCount = 0;
            for (var i = 1UL; i <= TestCondition.TEST_CASE_BITS; i++)
            {
                oneCount += (this._items[(int)(i - 1)] ? 1UL : 0UL);
                Assert.Equal<ulong>(oneCount, this._bv.Rank(true, i));
                Assert.Equal<ulong>(i - oneCount, this._bv.Rank(false, i));
            }

            Assert.Equal<ulong>(oneCount, this._bv.TotalPopCount());

            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.Rank(true, (ulong)this._items.Count + 1); });

            var random = new Random();

            for (var i = 0; i < TestCondition.SAMPLE_QUERY_CASE; i++)
            {
                var start = random.Next(0, this._items.Count / 2);
                var count = random.Next(1, this._items.Count - start + 1);

                oneCount = (ulong)this._items.Skip(start).Take(count).LongCount(c => c);
                Assert.Equal<ulong>(oneCount, this._bv.Rank(true, (ulong)count, (ulong)start));
                Assert.Equal<ulong>((ulong)count - oneCount, this._bv.Rank(false, (ulong)count, (ulong)start));
            }

            Assert.Throws<InvalidOperationException>(() => { this._bv.Rank(true, 0); });
            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.Rank(true, (ulong)this._items.Count, 5); });
            Assert.Throws<InvalidOperationException>(() => { this._bv.Rank(true, 0, 5); });
        }

        [Fact]
        public void AccessTest()
        {
            for (var i = 0; i < this._items.Count; i++)
                Assert.Equal<bool>(this._items[i], this._bv.Access((ulong)i));

            Assert.Equal<bool>(this._items[10], this._bv[10]);

            var temp = 10;
            foreach (var item in this._bv.AccessRange(10, 10))
            {
                Assert.Equal<bool>(this._items[temp], item);
                temp++;
            }
        }

        [Fact]
        public void SelectTest()
        {
            var oneCount = 0UL;
            var zeroCount = 0UL;

            for (var i = 0; i < TestCondition.TEST_CASE_BITS; i++)
            {
                if (this._items[i])
                {
                    oneCount++;
                    Assert.Equal<ulong>((ulong)i, this._bv.Select(true, oneCount));
                }
                else
                {
                    zeroCount++;
                    Assert.Equal<ulong>((ulong)i, this._bv.Select(false, zeroCount));
                }
            }

            Assert.Throws<InvalidOperationException>(() => { this._bv.Select(true, oneCount + 1); });
            Assert.Throws<InvalidOperationException>(() => { this._bv.Select(false, zeroCount + 1); });

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

                Assert.Equal<ulong>(indexOne, this._bv.Select(true, (ulong)rank, (ulong)start));
                Assert.Equal<ulong>(indexZero, this._bv.Select(false, (ulong)rank, (ulong)start));
            }

            Assert.Throws<InvalidOperationException>(() => { this._bv.Select(true, 0); });
            Assert.Throws<InvalidOperationException>(() => { this._bv.Select(false, 0, 10); });
            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.Select(true, 10, this._bv.Length); });
        }

        [Fact]
        public void SetTest()
        {
            var random = new Random();
            var popCount = this._bv.TotalPopCount();

            for (var i = 0UL; i < TestCondition.SAMPLE_QUERY_CASE; i++)
            {
                var ind = random.Next(0, this._items.Count);
                if (this._bv[(ulong)ind])
                {
                    // with method access
                    this._bv.Set((ulong)ind, false);
                    popCount--;
                }
                else
                {
                    // with indexer access
                    this._bv[(ulong)ind] = true;
                    popCount++;
                }
            }

            this._bv.BuildIndex();
            Assert.Equal<ulong>(this._bv.TotalPopCount(), popCount);

            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.Set(this._bv.Length + 1, false); });
        }

        [Fact]
        public void SetRangeTest()
        {
            var random = new Random();
            var popCount = this._bv.TotalPopCount();

            var segmentSize = (this._items.Count / 100);
            var operations = new List<Tuple<ulong, int>>();

            for (var i = 0; i < 100; i++)
            {
                var start = random.Next(i * segmentSize, (i + 1) * segmentSize - 5);
                var count = random.Next(1, ((i + 1) * segmentSize) - start);

                var rankInSegment = this._bv.Rank(true, (ulong)count, (ulong)start);
                popCount -= rankInSegment;

                operations.Add(new Tuple<ulong, int>((ulong)start, count));
            }

            foreach (var op in operations)
                this._bv.SetRange(op.Item1, new bool[op.Item2]);

            this._bv.BuildIndex();
            Assert.Equal<ulong>(popCount, this._bv.TotalPopCount());

            Assert.Throws<InvalidOperationException>(() => { this._bv.SetRange(0, new bool[0]); });
            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.SetRange(this._bv.Length + 1, new bool[10]); });
        }

        [Fact]
        public void ToggleTest()
        {
            var random = new Random();
            var popCount = this._bv.TotalPopCount();

            for (var i = 0UL; i < TestCondition.SAMPLE_QUERY_CASE; i++)
            {
                var ind = random.Next(0, this._items.Count);
                if (this._bv[(ulong)ind])
                {
                    popCount--;
                }
                else
                {
                    popCount++;
                }

                this._bv.Toggle((ulong)ind);
            }

            this._bv.BuildIndex();
            Assert.Equal<ulong>(this._bv.TotalPopCount(), popCount);

            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.Toggle(this._bv.Length + 1); });
        }

        [Fact]
        public void ToggleRangeTest()
        {
            var random = new Random();
            var popCount = this._bv.TotalPopCount();

            var segmentSize = (this._items.Count / 100);
            var operations = new List<Tuple<ulong, ulong>>();

            for (var i = 0; i < 100; i++)
            {
                var start = random.Next(i * segmentSize, (i + 1) * segmentSize - 5);
                var count = random.Next(1, ((i + 1) * segmentSize) - start);

                var rankInSegment = this._bv.Rank(true, (ulong)count, (ulong)start);
                popCount += (ulong)count - 2 * rankInSegment;

                operations.Add(new Tuple<ulong, ulong>((ulong)start, (ulong)count));
            }

            foreach (var op in operations)
                this._bv.ToggleRange(op.Item1, op.Item2);

            this._bv.BuildIndex();
            Assert.Equal<ulong>(popCount, this._bv.TotalPopCount());

            Assert.Throws<IndexOutOfRangeException>(() => { this._bv.ToggleRange(10, this._bv.Length - 9); });
            Assert.Throws<InvalidOperationException>(() => { this._bv.ToggleRange(10, 0); });

        }

        [Fact]
        public void ClearTest()
        {
            this._bv.Clear();
            for (var i = 0UL; i < TestCondition.TEST_CASE_BITS; i++)
                Assert.Equal<bool>(false, this._bv.Access(i));

            Assert.Equal<ulong>(0UL, this._bv.TotalPopCount());
            Assert.Equal<ulong>(TestCondition.TEST_CASE_BITS, this._bv.Length);
        }

        [Fact]
        public void ResizeTest()
        {
            var resizeLength = 10UL;
            this._bv.Resize(resizeLength);
            Assert.Equal<ulong>(0UL, this._bv.TotalPopCount());
            Assert.Equal<ulong>(resizeLength, this._bv.Length);
            for (var i = 0UL; i < resizeLength; i++)
                Assert.Equal<bool>(false, this._bv.Access(i));

            this._bv.Resize(TestCondition.TEST_CASE_BITS);
            Assert.Equal<ulong>(0UL, this._bv.TotalPopCount());
            Assert.Equal<ulong>(TestCondition.TEST_CASE_BITS, this._bv.Length);
        }

        [Fact()]
        public void ExtendTest()
        {
            var popCount = this._bv.TotalPopCount();

            var extendSize = 100UL;

            this._bv.Extend(extendSize);
            Assert.Equal<ulong>(popCount, this._bv.TotalPopCount());
            Assert.Equal<ulong>(TestCondition.TEST_CASE_BITS + extendSize, this._bv.Length);

            // Same as before in [..TestCondition.TEST_CASE_BITS)
            for (var i = TestCondition.TEST_CASE_BITS - 100; i < TestCondition.TEST_CASE_BITS; i++)
                Assert.Equal<bool>(this._items[i], this._bv[(ulong)i]);

            // All zero in extended region
            for (var i = (ulong)TestCondition.TEST_CASE_BITS; i < extendSize + TestCondition.TEST_CASE_BITS; i++)
                Assert.Equal<bool>(false, this._bv[i]);
        }

    }
}
