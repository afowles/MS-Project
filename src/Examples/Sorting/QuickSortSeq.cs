

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Examples.Sorting
{
    public class SortTesting
    {
        public static void Main2(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            //sw.Start();
            //GenerateRandomInts(25000000);
            //sw.Stop();
            //Console.WriteLine("Time for generating: " + sw.Elapsed);
            //sw.Reset();
            sw.Start();
            var nums = File.ReadAllLines("testlargeints.txt");
            sw.Stop();
            Console.WriteLine("Time for reading: " + sw.Elapsed);
            sw.Reset();
            sw.Start();
            QuickSortExtensions.QuicksortParallel(nums);
            sw.Stop();
            Console.WriteLine("Time for sorting: " + sw.Elapsed);
            sw.Reset();
            File.WriteAllLines("testlargeintsout.txt", nums);
            Console.ReadKey();
        }

        public static void GenerateRandomInts(int num)
        {
            Random rand = new Random(5);
            var ints = new List<string>();

            while (num > 0)
            {
                ints.Add(rand.Next().ToString());
                num--;
            }
            
            File.WriteAllLines("testlargeints.txt", ints);
        }
    }
}
