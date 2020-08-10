using System;
using System.Runtime.CompilerServices;
using System.Text;

#if NETCOREAPP3_1
using System.Runtime.Intrinsics.X86;

#endif

namespace BitCollections
{
    internal static class BitAlgorithms
    {
        internal static ReadOnlySpan<ulong> TrimTrailingZeroes(ReadOnlySpan<ulong> x)
        {
#if NETCOREAPP
            return x.TrimEnd(0ul);
#else
            var lenTrimmed = x.Length;
            while (lenTrimmed > 0 && x[lenTrimmed - 1] == 0) lenTrimmed--;
            return x.Slice(0, lenTrimmed);
#endif
        }

        internal static ulong GetFirstBitsOn(int count) =>
            count >= 64 ? ulong.MaxValue : (1ul << count) - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong AndNotSingle(ulong x1, ulong x2)
        {
#if NETCOREAPP3_1
            if (Bmi1.X64.IsSupported)
                // In the BMI instruction, it is the
                // first parameter that gets negated.
                return Bmi1.X64.AndNot(x2, x1);
#endif
            return x1 & ~ x2;
        }

        private static void ThrowDifferentLength() =>
            throw new ArgumentException("Spans have different length.");

        /// <summary>
        /// Calculates (<paramref name="dest"/> AND <paramref name="src"/>)
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool And(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            if (dest.Length != src.Length)
                ThrowDifferentLength();
            var changed = false;
            for (int i = 0; i < dest.Length; i++)
            {
                ref var cell = ref dest[i];
                var newValue = cell & src[i];
                changed |= cell != newValue;
                cell = newValue;
            }

            return changed;
        }

        /// <summary>
        /// Calculates (<paramref name="dest"/> OR <paramref name="src"/>)
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool Or(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            if (dest.Length != src.Length)
                ThrowDifferentLength();
            var changed = false;
            for (int i = 0; i < dest.Length; i++)
            {
                ref var cell = ref dest[i];
                var newValue = cell | src[i];
                changed |= cell != newValue;
                cell = newValue;
            }

            return changed;
        }

        /// <summary>
        /// Calculates (<paramref name="dest"/> XOR <paramref name="src"/>)
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool Xor(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            if (dest.Length != src.Length)
                ThrowDifferentLength();
            var changed = false;
            for (int i = 0; i < dest.Length; i++)
            {
                ref var cell = ref dest[i];
                var newValue = cell ^ src[i];
                changed |= cell != newValue;
                cell = newValue;
            }

            return changed;
        }

        /// <summary>
        /// Calculates (<paramref name="dest"/> AND (NOT <paramref name="src"/>))
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool AndNot(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            if (dest.Length != src.Length)
                ThrowDifferentLength();
            var changed = false;
            for (int i = 0; i < dest.Length; i++)
            {
                ref var cell = ref dest[i];
                var newValue = AndNotSingle(cell, src[i]);
                changed |= cell != newValue;
                cell = newValue;
            }

            return changed;
        }

        /// <summary>
        /// Inverts the bits of <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The bits to invert.</param>
        internal static void Not(Span<ulong> x)
        {
            for (int i = 0; i < x.Length; i++)
                x[i] = ~x[i];
        }

        internal static string FormatBitArray(ulong first, ReadOnlySpan<ulong> rest)
        {
            var sb = new StringBuilder();
            for (int i = rest.Length - 1; i >= 0; i--)
            {
                sb.Append(rest[i].ToString("x16")).Append('-');
            }
            sb.Append(first.ToString("X16"));
            return sb.ToString();
        }
    }
}
