

//using System.Xml.Serialization;

using System;
using System.Diagnostics;

namespace SubmitJob
{
    public class SubmitJob
    {
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Distributed.SubmitJob.Main(args);
            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("        " + "HR:MM:SS:MS");
            Console.WriteLine("RunTime " + elapsedTime);
            Console.ReadKey();

        }
    }
}
