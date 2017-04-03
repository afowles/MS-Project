using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Examples.Sorting
{
    public static class QuickSortSeq
    {

        private static void QuicksortSequential<T>(this IList<T> arr, int left, int right) where T : IComparable<T>
        {
            if (right > left)
            {

                //int pivot = Partition(arr, left, right);
                //QuicksortSequential(arr, left, pivot - 1);
                //QuicksortSequential(arr, pivot + 1, right);
            }
        }
    }
}
