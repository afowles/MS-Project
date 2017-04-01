using System;
using System.Diagnostics;
using Distributed.Library;
using System.Numerics;
using System.Collections.Generic;

namespace DebugApplicationUsingPDLib
{
    public class TestPrimeNumbers : Job
    {
        private List<BigInteger> PrimesToTest;

        public TestPrimeNumbers()
        {
            PrimesToTest = new List<BigInteger>();

            PrimesToTest.Add(new BigInteger(10000000000183));
            PrimesToTest.Add(new BigInteger(961748927));
            PrimesToTest.Add(new BigInteger(941083987));
            PrimesToTest.Add(new BigInteger(920419823));
            PrimesToTest.Add(new BigInteger(899809363));
            PrimesToTest.Add(new BigInteger(879190841));
            PrimesToTest.Add(new BigInteger(879190747));
            PrimesToTest.Add(new BigInteger(858599509));
            PrimesToTest.Add(new BigInteger(472882027));
            PrimesToTest.Add(new BigInteger(256203221));
        }

        public override void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                AddTask(new TestAppJobTask(PrimesToTest[i]));
            }
            
        }

    }

    public class TestAppJobTask : JobTask
    {
        private BigInteger TestBigInt;
        public TestAppJobTask(BigInteger b)
        {
            TestBigInt = b;
        }

        public override void Main(string[] args)
        {
            for(BigInteger i = 3; i < TestBigInt.Sqrt(); i++)
            {
                if (BigInteger.Remainder(TestBigInt, i) == 0)
                {
                    Console.WriteLine(TestBigInt + " is not prime");
                }
            }
            Console.WriteLine(TestBigInt + " is prime");
        }

        


    }

    public static class Extensions
    {
        public static BigInteger Sqrt(this BigInteger n)
        {
            if (n == 0) return 0;
            if (n > 0)
            {
                int bitLength = Convert.ToInt32(Math.Ceiling(BigInteger.Log(n, 2)));
                BigInteger root = BigInteger.One << (bitLength / 2);

                while (!IsSqrt(n, root))
                {
                    root += n / root;
                    root /= 2;
                }

                return root;
            }
            // won't happen if n is a number
            return new BigInteger(0);
        }
        private static bool IsSqrt(BigInteger n, BigInteger root)
        {
            BigInteger lowerBound = root * root;
            BigInteger upperBound = (root + 1) * (root + 1);

            return (n >= lowerBound && n < upperBound);
        }
    }
    public class test
    {
        public static void Main(string[] args)
        {
            TestPrimeNumbers tp = new TestPrimeNumbers();
            tp.Main(args);
            BigInteger b = new BigInteger(2);
           
            for(int i = 0; i < 10; i++)
            {
                tp.startTask(i);
            }
            //Console.WriteLine(test::name);
            Console.ReadKey();
        }
    }

}
