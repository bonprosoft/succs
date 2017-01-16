using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SucCs.BitVector
{
    public static class BitUtility
    {
        // ref: http://d.hatena.ne.jp/mclh46/20100408/1270737141
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint PopCountInt64(ulong bits)
        {
            bits -= (bits >> 1) & 0x5555555555555555UL;
            bits = (bits & 0x3333333333333333UL) + ((bits >> 2) & 0x3333333333333333UL);

            return (uint)((((bits + (bits >> 4)) & 0x0f0f0f0f0f0f0f0fUL) * 0x0101010101010101UL) >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RankInt64(ulong bits, bool value)
        {
            if (!value) bits = ~bits;

            return PopCountInt64(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RankInt64(ulong bits, int offset, bool value)
        {
            if (offset == 0) return 0;
            if (!value) bits = ~bits;

            return PopCountInt64(bits & ~((1UL << (64 - offset)) - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RankInt64OfLowerMask(ulong bits, int upLimitOffset, bool value)
        {
            if (!value) bits = ~bits;

            return PopCountInt64(bits & ((1UL << upLimitOffset) - 1));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SelectInt64(ulong bits, uint rank)
        {
            var x1 = ((bits & 0xaaaaaaaaaaaaaaaaUL) >> 1)
                + (bits & 0x5555555555555555UL);
            var x2 = ((x1 & 0xccccccccccccccccUL) >> 2)
                + (x1 & 0x3333333333333333UL);
            var x3 = ((x2 & 0xf0f0f0f0f0f0f0f0UL) >> 4)
                + (x2 & 0x0f0f0f0f0f0f0f0fUL);
            var x4 = ((x3 & 0xff00ff00ff00ff00UL) >> 8)
                + (x3 & 0x00ff00ff00ff00ffUL);
            var x5 = ((x4 & 0xffff0000ffff0000UL) >> 16)
                + (x4 & 0x0000ffff0000ffffUL);

            var i = (ulong)rank;

            var pos = 0;
            var v5 = x5 & 0xffffffffUL;
            if (i > v5) { i -= v5; pos += 32; }
            var v4 = (x4 >> pos) & 0x0000ffffUL;
            if (i > v4) { i -= v4; pos += 16; }
            var v3 = (x3 >> pos) & 0x000000ffUL;
            if (i > v3) { i -= v3; pos += 8; }
            var v2 = (x2 >> pos) & 0x0000000fUL;
            if (i > v2) { i -= v2; pos += 4; }
            var v1 = (x1 >> pos) & 0x00000003UL;
            if (i > v1) { i -= v1; pos += 2; }
            var v0 = (bits >> pos) & 0x00000001UL;
            if (i > v0) { i -= v0; pos += 1; }

            return (uint)pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SelectInt64(ulong bits, bool value, uint rank)
        {
            if (!value) bits = ~bits;

            return SelectInt64(bits, rank);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseInt64(ulong value)
        {
            value = (value & 0x5555555555555555UL) << 1 | (value >> 1) & 0x5555555555555555UL;
            value = (value & 0x3333333333333333UL) << 2 | (value >> 2) & 0x3333333333333333UL;
            value = (value & 0x0f0f0f0f0f0f0f0fUL) << 4 | (value >> 4) & 0x0f0f0f0f0f0f0f0fUL;
            value = (value & 0x00ff00ff00ff00ffUL) << 8 | (value >> 8) & 0x00ff00ff00ff00ffUL;
            // rotation method is faster than bit-operation
            return (value << 48) | ((value & 0xffff0000UL) << 16) | ((value >> 16) & 0xffff0000UL) | (value >> 48);

            //value = (value & 0x0000ffff0000ffffUL) << 16 | (value >> 16) & 0x0000ffff0000ffffUL;
            //return ((value & 0x00000000ffffffffUL) << 32) | ((value >> 32) & 0x00000000ffffffffUL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseInt32(uint value)
        {
            value = (value & 0x55555555U) << 1 | (value >> 1) & 0x55555555U;
            value = (value & 0x33333333U) << 2 | (value >> 2) & 0x33333333U;
            value = (value & 0x0f0f0f0fU) << 4 | (value >> 4) & 0x0f0f0f0fU;
            // rotation method is faster than bit-operation
            return (value << 24) | ((value & 0xff00U) << 8) | ((value >> 8) & 0xff00U) | (value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BoolToUlong(bool[] value, ulong index = 0UL)
        {
            var maxIndex = (ulong)value.Length;
            var result = 0UL;
            for (var i = index; i < index + 64; i++)
            {
                if (i < maxIndex && value[i])
                {
                    result = (result << 1) | 1UL;
                }
                else
                {
                    result = (result << 1) & ~1UL;
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BoolToUlong(IList<bool> value, int index = 0)
        {
            var maxIndex = value.Count;
            var result = 0UL;
            for (var i = index; i < index + 64; i++)
            {
                if (i < maxIndex && value[i])
                {
                    result = (result << 1) | 1UL;
                }
                else
                {
                    result = (result << 1) & ~1UL;
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong UIntToUlong(uint[] value, ulong index = 0UL)
        {
            var maxIndex = (ulong)value.Length;
            if (index >= maxIndex)
                return 0UL;

            var result = (ulong)value[index] << 32;
            index++;
            if (index >= maxIndex)
                return result;

            return result | value[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong UIntToUlong(IList<uint> value, int index = 0)
        {
            var maxIndex = value.Count;
            if (index >= maxIndex)
                return 0UL;

            var result = (ulong)value[index] << 32;
            index++;
            if (index >= maxIndex)
                return result;

            return result | value[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool[] ULongToBool(ulong n)
        {
            var value = new bool[64];

            for (var i = 0; i < 64; i++)
                value[63 - i] = ((n & (1UL << i)) != 0);

            return value;
        }

    }
}
