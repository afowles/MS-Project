
using System;
using Distributed.Library;
using System.Numerics;

namespace Examples.Primes
{
    /// <summary>
    /// A class to run through a distributed system
    /// to verify goldbach's conjecture. See GoldbachSeq
    /// </summary>
    public class GoldbachDis : Job
    {
        /// <summary>
        /// Overridden non-static Main
        /// </summary>
        /// <param name="args_full"></param>
        public override void Main(string[] args_full)
        {
            foreach (string s in args_full)
            {
                // split the input
                string[] args = s.Split(',');
                // create a new task for that input
                AddTask(new GoldbachTask(args));
            }
        }

        /// <summary>
        /// A class to represent the task of verifying
        /// one set of integers for Goldbach.
        /// </summary>
        public class GoldbachTask : JobTask
        {
            private BigInteger upperBound, lowerBound, largestFound, sumForLargest;
            private int certainty = 100;
            private string[] args;

            /// <summary>
            /// Constructor for the Goldbach Task
            /// </summary>
            /// <param name="args"></param>
            public GoldbachTask(string[] args)
            {
                this.args = args;
            }

            /// <summary>
            /// See sequential.
            /// </summary>
            /// <param name="sum"></param>
            private void FindTwoPrimeSummation(BigInteger sum)
            {
                BigInteger currentPrime = 2;
                while (true)
                {
                    // If q is prime
                    //if ((sum.subtract(currentPrime)).isProbablePrime(certainty))
                    if (BigInteger.Subtract(sum, currentPrime).IsProbablePrime(certainty))
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


            /// <summary>
            /// Main method for the GoldbachSeq task
            /// </summary>
            /// <param name="args_full">
            /// command line args passed to this main
            ///             lowerbound - an even integer > two
            ///             upperbound - an even integer >= lowerbound
            /// </param>
            public override void Main(string[] args2)
            {
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
                    FindTwoPrimeSummation(i);
                }
                // Print out the largest found
                Console.WriteLine(sumForLargest);
                Console.WriteLine(" = ");
                Console.WriteLine(largestFound);
                Console.WriteLine(" + ");
                Console.WriteLine(sumForLargest - largestFound);
                Console.WriteLine();
            }

            private void usage(String error)
            {
                Console.WriteLine(error +
                        "\nUsage: GoldbachSeq <lb> <ub>");
            }
        }
    }
}
