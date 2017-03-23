using Examples.Primes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Examples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GoldbachSeq q = new GoldbachSeq();
            q.main(args);
            Console.ReadKey();
        }
    }
}
