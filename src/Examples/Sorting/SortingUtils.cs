using System;

using System.Threading.Tasks;

namespace Examples.Sorting
{
    using static SortUtils;

    public static class SortUtils
    {
        /// <summary>
        /// Defines the cutoff threshold at which point QuickSort will revert to Insertion Sort, derived from testing
        /// </summary>
        internal const uint INSERTION_SORT_CUTOFF = 47;

        /// <summary>
        /// Defines the cutoff threshold at which point Parallel Quicksort will revert to Sequential QuickSort, derived from testing
        /// </summary>
        internal const uint SEQUENTIAL_CUTOFF = 286;

        private static readonly Random shufflingRandom = new Random();

        /// <summary>
        /// Performs Fisher-Yates Shuffle on this array.
        /// </summary>
        /// <param name="rand">PRNG to use for index selection</param>
        public static void Shuffle<T>(this T[] arr, Random rand)
        {
            for (var i = 0; i < arr.Length - 1; i++)
            {
                var k = rand.Next(i, arr.Length);
                Swap(ref arr[i], ref arr[k]);
            }
        }

        /// <summary>
        /// Performs Fisher-Yates Shuffle on this array. Uses shared "shuffling" PRNG seeded initially by the time of initialization.
        /// </summary>
        public static void Shuffle<T>(this T[] arr)
            => arr.Shuffle(shufflingRandom);

        /// <summary>
        /// Determines if this array is sorted in ascending order.
        /// </summary>
        /// <returns>True if sorted in ascending order, false otherwise</returns>
        public static bool IsSorted<T>(this T[] arr) where T : IComparable<T>
        {
            for (var i = 0; i < arr.Length - 1; i++)
                if (arr[i].CompareTo(arr[i + 1]) > 0)
                    return false;

            return true;
        }

        /// <summary>
        /// Swaps the values of the two references.
        /// </summary>
        /// <param name="a">First Reference</param>
        /// <param name="b">Second Reference</param>
        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

    }

    /// <summary>
    /// Array Extensions for supporting InsertionSort on any array of Comparables.
    /// </summary>
    public static class InsertionSortExtensions
    {
        public static void InsertionSort<T>(this T[] arr) where T : IComparable<T>
            => arr.InsertionSort(0, arr.Length - 1);

        public static void InsertionSort<T>(this T[] arr, int start, int end) where T : IComparable<T>
        {
            for (int i = start, k = start; i < end + 1; k = ++i)
                while (k > start && arr[k - 1].CompareTo(arr[k]) > 0)
                    Swap(ref arr[k - 1], ref arr[k--]);
        }
    }

    /// <summary>
    /// Array Extensions for supporting QuickSort (both Sequential and Parallel) on any array of Comparables.
    /// </summary>
    public static class QuickSortExtensions
    {
        public static void QuicksortSequential<T>(this T[] arr) where T : IComparable<T>
            => arr.QuicksortSequential(0, arr.Length - 1);

        public static void QuicksortSequential<T>(this T[] arr, int start, int end) where T : IComparable<T>
        {
            if (start < end)
            {
                int low, high, nextStart, nextEnd;
                arr.Partition(start, end, out low, out high);

                if (low - start > end - high)
                {
                    nextStart = high + 1;
                    nextEnd = end;
                    arr.QuicksortSequential(start, low - 1);
                }
                else
                {
                    nextStart = start;
                    nextEnd = low - 1;
                    arr.QuicksortSequential(high + 1, end);
                }

                if (nextEnd - nextStart <= INSERTION_SORT_CUTOFF)
                    arr.InsertionSort(nextStart, nextEnd);
                else
                    arr.QuicksortSequential(nextStart, nextEnd);
            }
        }

        public static void QuicksortParallel<T>(this T[] arr) where T : IComparable<T>
            => arr.QuicksortParallel(0, arr.Length - 1);

        public static void QuicksortParallel<T>(this T[] arr, int start, int end) where T : IComparable<T>
        {
            if (end - start <= INSERTION_SORT_CUTOFF)
                arr.InsertionSort(start, end);
            else if (end - start <= SEQUENTIAL_CUTOFF)
                arr.QuicksortSequential(start, end);
            else
            {
                int low, high;
                arr.Partition(start, end, out low, out high);

                Parallel.Invoke(
                    () => arr.QuicksortParallel(start, low - 1),
                    () => arr.QuicksortParallel(high + 1, end)
                );
            }
        }

        /// <summary>
        /// Partitions the given array using a variant of Hoare Paritioning with a modification that groups duplicate pivot elements. Two values are retuned via out parameters,
        /// the 'high' and 'low' pointers. The 'high' pointer points to the element before the start of the greater-than list while the 'low' pointer points to the 
        /// element after the end of the less-than list. All values between the indices pointed to by 'high' and 'low' are equal to the pivot element and thusly sorted.
        /// </summary>
        /// <param name="arr">Array to partition</param>
        /// <param name="start">Start index of sub-array</param>
        /// <param name="end">End index of sub-array</param>
        /// <param name="low">Low Pointer result output</param>
        /// <param name="high">High Pointer result output</param>
        private static void Partition<T>(this T[] arr, int start, int end, out int low, out int high) where T : IComparable<T>
        {
            var pivot = arr.Pivot(start, end);

            var i = start;  // Start of Duplicate Pivot Elements
            var k = start;  // Marching Pointer
            var n = end;    // End of Duplicate Pivot Elements

            while (k <= n)
            {
                if (arr[k].CompareTo(pivot) < 0)         // Move elements less than pivot to before start of pivot segment
                {
                    Swap(ref arr[i], ref arr[k]);
                    i++;
                    k++;
                }
                else if (arr[k].CompareTo(pivot) > 0)    // Move elements greater than pivot to after end of pivot segment
                {
                    Swap(ref arr[k], ref arr[n]);
                    n--;
                }
                else                                    // Pivot Element found, advance marching pointer
                    k++;
            }

            low = i;
            high = n;
        }

        /// <summary>
        /// Select Pivot element from subrange of array using Median of Three pivot selection.
        /// </summary>
        /// <param name="arr">Array to select pivot from</param>
        /// <param name="start">Start index of subrange</param>
        /// <param name="end">End index of subrange</param>
        /// <returns></returns>
        private static T Pivot<T>(this T[] arr, int start, int end) where T : IComparable<T>
        {
            int center = (start + end) / 2;

            // Order center, start and end elements correctly
            if (arr[center].CompareTo(arr[start]) < 0)
                Swap(ref arr[center], ref arr[start]);
            if (arr[end].CompareTo(arr[start]) < 0)
                Swap(ref arr[end], ref arr[start]);
            if (arr[end].CompareTo(arr[center]) < 0)
                Swap(ref arr[end], ref arr[center]);

            // Select Center as pivot since it's most likely to be the median
            return arr[center];
        }
    }

}