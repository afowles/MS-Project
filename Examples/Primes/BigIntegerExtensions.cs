using System;
using System.Collections;
using System.Numerics;

/// <summary>
/// The extension method isProbablePrime is based
/// on java's implementation http://developer.classpath.org/doc/java/math/BigInteger-source.html
/// with use of HAC (Handbook of Applied Cryptography), Alfred Menezes & al.
/// http://cacr.uwaterloo.ca/hac/
/// </summary>
namespace Examples.Primes
{
    /// <summary>
    /// Extension Methods for BigInteger
    /// </summary>
    public static class BigIntegerExtensions
    {
        /// <summary>
        /// An array of small primes
        /// </summary>
        static int[] primes =
        {
            2,   3,   5,   7,  11,  13,  17,  19,  23,  29,  31,  37,  41,  43,
            47,  53,  59,  61,  67,  71,  73,  79,  83,  89,  97, 101, 103, 107,
            109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181,
            191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251
        };

        /// <summary>
        /// HAC (Handbook of Applied Cryptography), Alfred Menezes & al. Table 4.4.
        /// </summary>
        static int[] k =
        {
            100,150,200,250,300,350,400,500,600,800,1250,int.MaxValue
        };
        static int[] t =
        {
            27,18,15,12,9,8,7,6,5,4,3,2
        };

        private static int minFixNum = -100;
        private static int maxFixNum = 1024;
        private static int numFixNum = maxFixNum - minFixNum + 1;
        private static BigInteger[] smallFixNums = new BigInteger[numFixNum];

        /// <summary>
        /// Statically construct smallFixNums, this is called 
        /// before any method is used
        /// </summary>
        static BigIntegerExtensions()
        {
            for (int i = numFixNum; --i >= 0;)
                smallFixNums[i] = new BigInteger(i + minFixNum);
        }

        /// <summary>
        /// Millar-Rabin test for probabilistic prime
        /// Based on java's implementation http://developer.classpath.org/doc/java/math/BigInteger-source.html
        /// </summary>
        /// <param name="bigInt"></param>
        /// <param name="certainty"></param>
        /// <returns></returns>
        public static bool isProbablePrime(this BigInteger thisBigInt, int certainty)
        {
            if (certainty < 1) { return true; }
            // handle trivial cases
            if (thisBigInt == 2 || thisBigInt == 3) { return true; };
            if (thisBigInt < 2 || thisBigInt % 2 == 0) { return false; }
                
            /** We'll use the Rabin-Miller algorithm for doing a probabilistic
             * primality test.  It is fast, easy and has faster decreasing odds of a
             * composite passing than with other tests.  This means that this
             * method will actually have a probability much greater than the
             * 1 - .5^certainty specified in the JCL (p. 117), but I don't think
             * anyone will complain about better performance with greater certainty.
                   * The Rabin-Miller algorithm can be found on pp. 259-261 of "Applied
             * Cryptography, Second Edition" by Bruce Schneier.
             */

            //BigInteger pMinus1 = add(this, -1);
            BigInteger pMinus1 = BigInteger.Add(thisBigInt, -1);
            BigInteger m = BigInteger.Add(thisBigInt, -1);

            //int b = pMinus1.getLowestSetBit();
            // Set m such that this = 1 + 2^b * m.
            //BigInteger m = pMinus1.divide(valueOf(2L << b - 1));
            int b = 0;
            while (m % 2 == 0)
            {
                m /= 2;
                b += 1;
            }


            // The HAC (Handbook of Applied Cryptography), Alfred Menezes & al. Note
            // 4.49 (controlling the error probability) gives the number of trials
            // for an error probability of 1/2**80, given the number of bits in the
            // number to test.  we shall use these numbers as is if/when 'certainty'
            // is less or equal to 80, and twice as much if it's greater.
            //int bits = this.bitLength();
            int bits = thisBigInt.BitCount();
            int i;
            for (i = 0; i < k.Length; i++)
                if (bits <= k[i])
                    break;
            int trials = t[i];
            if (certainty > 80)
                trials *= 2;
            BigInteger z;
            for (int t = 0; t < trials; t++)
            {
                // The HAC (Handbook of Applied Cryptography), Alfred Menezes & al.
                // Remark 4.28 states: "...A strategy that is sometimes employed
                // is to fix the bases a to be the first few primes instead of
                // choosing them at random.
                //z = smallFixNums[primes[t] - minFixNum].ModPow(m, this);
                z = BigInteger.ModPow(smallFixNums[primes[t] - minFixNum], m, thisBigInt);
                if (z.IsOne || z == pMinus1)
                    continue;            // Passes the test; may be prime.

                for (i = 0; i < b;)
                {
                    if (z.IsOne)
                        return false;
                    i++;
                    if (z == pMinus1)
                        break;            // Passes the test; may be prime.

                    //z = z.modPow(valueOf(2), this);
                    z = BigInteger.ModPow(z, 2, thisBigInt);
                }
                if (i == b && !(z == pMinus1))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Counts the number of bits in
        /// a big integer
        /// http://stackoverflow.com/questions/2709430/count-number-of-bits-in-a-64-bit-long-big-integer
        /// </summary>
        /// <param name="number">number to count</param>
        /// <returns></returns>
        public static int BitCount(this BigInteger number)
        {
            int ret = 0;
            // Every time you &= 
            // a number with itself minus one you
            // eliminate the last set bit in that number.
            while (number != 0)
            {
                number &= (number - 1);
                ret++;
            }
            return ret;
        }

        /// <summary>
        /// Finds the next probably prime
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static BigInteger NextProbablePrime(this BigInteger number)
        {
            BigInteger ret = number;
            while(true)
            {
                ret += 1;
                if (ret.isProbablePrime(100))
                {
                    return ret;
                }
            }
        }  

    }
}
