using System;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_1
using System.Runtime.Intrinsics.X86;

#endif

namespace BitCollections
{
    internal static class BitAlgorithms
    {
        internal static ReadOnlySpan<ulong> TrimTrailingZeroes(ReadOnlySpan<ulong> x)
        {
            var lenTrimmed = x.Length;
            while (lenTrimmed > 0 && x[lenTrimmed - 1] == 0) lenTrimmed--;
            return x.Slice(0, lenTrimmed);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckForSameLength(ReadOnlySpan<ulong> x1, ReadOnlySpan<ulong> x2)
        {
            if (x1.Length != x2.Length)
                throw new ArgumentException("Spans have different length.");
        }

        /// <summary>
        /// Calculates (<paramref name="dest"/> AND <paramref name="src"/>)
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool And(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            CheckForSameLength(dest, src);
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
            CheckForSameLength(dest, src);
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
        /// Calculates (<paramref name="dest"/> AND (NOT <paramref name="src"/>))
        /// to each of the elements of <paramref name="dest"/> and <paramref name="src"/>
        /// and stores the result to <paramref name="dest"/>.
        /// </summary>
        /// <returns>Whether the content of <paramref name="dest"/> changed.</returns>
        internal static bool AndNot(Span<ulong> dest, ReadOnlySpan<ulong> src)
        {
            CheckForSameLength(dest, src);
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
    }
}
