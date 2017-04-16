using System;
using System.IO;
using System.Runtime.CompilerServices;
using Defcore.Distributed.IO;
using Defcore.Distributed.Jobs;

[assembly: InternalsVisibleTo("Loader")]

namespace Defcore.Distributed.Assembly
{
    internal class JobExecuter
    {
        /// <summary>
        /// Loads and runs the users program
        /// </summary>
        /// <param name="args">[0] is the DLL path, 
        /// [1] is any library args separated by ','
        /// The rest are user arguments to their program</param>
        public static void Main(string[] args)
        {
            // Save off the console.out text writer
            // before changing it, to restore at the end.
            var originalOut = Console.Out;
            // Set the console output writer to a text writer
            // to capture output.
            var textWriter = new JobTextWriter();
            Console.SetOut(textWriter);

         
            // Create a place for user arguments
            var userArgs = new string[args.Length - 2];
            //Console.WriteLine("Arguments: ");
            for (var i = 0; i < args.Length - 2; i++)
            {
                Console.WriteLine(args[i+2]);
                userArgs[i] = args[i + 2];
            }
            var cwd = Directory.GetCurrentDirectory();
            try
            {

            
                // parse the arguments needed for proper execution
                var libArgs = ParseTokens(args);

                // Create an assembly loader object for the users program
                var coreLoader = new CoreLoader<Job>(cwd + "\\" + args[0]);
                // Find the Job Class, this must exist to run distributed.
            
            
                // If they inherited the Job class it will have Main.
                // Call it with the user arguments. Nothing
                // is exepected back from Main but an object result is returned
                // from the call method function.
                coreLoader.CallMethod("Main", new object[] { userArgs });
                
                // This method does not require arguments.
                // Calls Count on the internal task list being kept by the job.
                var numTasks = (int)coreLoader.CallMethod("GetNumberTasks", new object[] { });
                
                // schedule does not take any arguments
                var schedule = (Schedule)coreLoader.CallMethod("GetSchedule", new object[] { });
                
                switch (schedule)
                {
                    case Schedule.FIXED:
                        RunFixedSchedule(coreLoader, numTasks, libArgs);
                        break;
                    default:
                        Console.WriteLine("Default called..." + schedule);
                        break;
                }
                // restore the output stream
                Console.SetOut(originalOut);
                // output the result as a json object
                Console.WriteLine("Output is...: " + textWriter.GetJsonResult());
                
                //coreLoader.CallMethod()

            }
            catch (Exception e)
            {
                Console.SetOut(originalOut);
                Console.WriteLine(e);
                Console.WriteLine("WHAT: " + e.Message);
                Console.WriteLine(textWriter.GetJsonResult());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int[] ParseTokens(string[] args)
        {
            // jobid, sectionid, number of nodes
            var result = new int[3];
            // split on comma
            var s = args[1].Split(',');
            foreach (var so in s)
            {
                Console.WriteLine(so);
            }
            result[0] = int.Parse(s[0]);
            result[1] = int.Parse(s[1]);
            result[2] = int.Parse(s[2]);
            return result;

        }

        /// <summary>
        /// Run the program with a fixed schedule
        /// </summary>
        /// <remarks>
        /// libArgs = [job_id, section_id, num_nodes]
        /// </remarks>
        public static void RunFixedSchedule(CoreLoader<Job> coreLoader, int numTasks, int[] libArgs)
        {
            var fixedBlock = numTasks / libArgs[2];
            Console.WriteLine("FixedBlock = " + fixedBlock);
            var start = fixedBlock * libArgs[1];
            Console.WriteLine("Start = " + start);
            var end = start + fixedBlock;
            if (end > numTasks)
            {
                end = numTasks;
            }
            Console.WriteLine("End = " + end);
            for (var i = start; i < end; i++)
            {
                Console.WriteLine("Trying to start a task");
                coreLoader.CallMethod("StartTask", new object[] { i });
            }
        }
    }
}
