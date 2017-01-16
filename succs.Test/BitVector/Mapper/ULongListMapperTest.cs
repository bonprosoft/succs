using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SucCs.BitVector;
using SucCs.BitVector.Mapper;

namespace SucCs.Test.BitVector.Mapper
{
    public class ULongListMapperTest : BitIndexTestBase<ULongListMapper>
    {
        protected override void InitializeBitIndexInstance()
        {
            var temp = Enumerable.Range(0, (int)((TestCondition.TEST_CASE_BITS + 64 - 1) / 64))
                                 .Select(c => BitUtility.BoolToUlong(this._items, c * 64)).ToList();

            this._bi = new BitIndex<ULongListMapper>(new ULongListMapper(temp), true);
            this._largeBi = new UShortBitIndex<ULongListMapper>(new ULongListMapper(temp), true);
        }
    }
}
