using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SucCs.BitVector;
using SucCs.BitVector.Mapper;

namespace SucCs.Test.BitVector.Mapper
{
    public class UIntListMapperTest : BitIndexTestBase<UIntListMapper>
    {
        protected override void InitializeBitIndexInstance()
        {
            var temp = Enumerable.Range(0, (int)((TestCondition.TEST_CASE_BITS + 32 - 1) / 32))
                                 .Select(c => BoolToUInt(this._items, c * 32)).ToList();

            this._bi = new BitIndex<UIntListMapper>(new UIntListMapper(temp), true);
            this._largeBi = new UShortBitIndex<UIntListMapper>(new UIntListMapper(temp), true);
        }

        private uint BoolToUInt(IList<bool> value, int index)
        {
            var maxIndex = value.Count;
            var result = 0U;
            for (var i = index; i < index + 32; i++)
            {
                if (i < maxIndex && value[i])
                {
                    result = (result << 1) | 1U;
                }
                else
                {
                    result = (result << 1) & ~1U;
                }
            }
            return result;
        }



    }
}
