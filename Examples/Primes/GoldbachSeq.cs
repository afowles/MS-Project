using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Examples.Primes
{
    public class GoldbachSeq
    {
        private BigInteger upperBound, lowerBound, largestFound, sumForLargest;
        private int certainty = 100;

        /**
         *
         * Main method for the GoldbachSeq task
         * @param args - command line args passed to this main
         *             lowerbound - an even integer > two
         *             upperbound - an even integer >= lowerbound
         * @throws Exception - throwing Illegal Argument exception terminates program
         */
        public void main(String[] args_full)
        {
            foreach (string s in args_full)
            {
                string[] args = s.Split(',');
                Console.WriteLine(args[0] + " " + args[1]);
                // Set the largest found
                largestFound = 0;
                // Error conditions
                if (args.Length != 2)
                {
                    usage("Incorrect number of arguments");
                }

                // Java approach would be as follows
                //try { lowerBound = new BigInteger(args[0]); }
                //catch (NumberFormatException e) { usage("Incorrect lower bound argument given (must be int)"); }

                // C# approach
                bool success = BigInteger.TryParse(args[0], out lowerBound);
                if (!success) { usage("Incorrect lower bound argument given (must be int)"); }

                success = BigInteger.TryParse(args[1], out upperBound);
                if (!success) { usage("Incorrect upper bound argument given (must be int)"); }
                //try { upperBound = new BigInteger(args[1]); }
                //catch (NumberFormatException e) { usage("Incorrect upper bound argument given (must be int)"); }

                if (lowerBound <= 2) { usage("Lower bound must be > 2 "); }

                if (!(lowerBound % 2 == 0)) { usage("Lower bound must be even"); }

                if (upperBound < lowerBound) { usage("Upper bound must be >= lower bound"); }

                if (!(upperBound % 2 == 0)) { usage("Upper bound must be even"); }

                sumForLargest = 0;
                // Iterate over all even integers between <lb> and <ub>
                for (BigInteger i = lowerBound; i < upperBound; i += 2)
                {
                    findTwoPrimeSummation(i);
                }
                // Print out the largest found
                Console.WriteLine(sumForLargest);
                Console.WriteLine(" = ");
                Console.WriteLine(largestFound);
                Console.WriteLine(" + ");
                Console.WriteLine(sumForLargest - largestFound);
                Console.WriteLine();
            }
        }

        /**
         * Method to compute the smallest prime p such that
         * p + q = sum where q is also prime.
         * @param sum - the sum of two primes
         *            assuming Goldbach's conjecture.
         */
        private void findTwoPrimeSummation(BigInteger sum)
        {
            BigInteger currentPrime = 2;
            while (true)
            {
                // If q is prime
                //if ((sum.subtract(currentPrime)).isProbablePrime(certainty))
                if(BigInteger.Subtract(sum, currentPrime).isProbablePrime(certainty))
                {
                    // If the current prime is >= largest found
                    //if (currentPrime.compareTo(largestFound) >= 0)
                    if (currentPrime >= largestFound)
                    {
                        // if the current prime is equal to the largest
                        //if (currentPrime.compareTo(largestFound) == 0)
                        if (currentPrime == largestFound)
                        {
                            // Pick the one with the greater i (sum)
                            //if (sum.compareTo(sumForLargest) > 0)
                            if (sum > sumForLargest)
                            {
                                largestFound = currentPrime;
                                sumForLargest = sum;
                            }
                        }
                        else
                        {
                            largestFound = currentPrime;
                            sumForLargest = sum;
                        }
                    }
                    return;

                }
                currentPrime = currentPrime.NextProbablePrime();
            }
        }

        /**
         * Prints out the error + usage message then exits
         * by throwing an illegal argument exception since
         * calling system.exit() causes problems
         * @param error - an error with command line arguments
         *
         */
        private void usage(String error)
        {
            Console.WriteLine(error +
                    "\nUsage: GoldbachSeq <lb> <ub>");
        }
    }
}
