using System;
using System.Collections.Generic;
using System.Text;
using SucCs.BitVector;
using SucCs.BitVector.Mapper;

namespace SucCs.Test.BitVector.Mapper
{
    public class BoolListMapperTest : BitIndexTestBase<BoolListMapper>
    {
        protected override void InitializeBitIndexInstance()
        {
            this._bi = new BitIndex<BoolListMapper>(new BoolListMapper(this._items), true);
            this._largeBi = new UShortBitIndex<BoolListMapper>(new BoolListMapper(this._items), true);
        }
    }
}
