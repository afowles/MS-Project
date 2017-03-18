using System;
using System.Reflection;
using System.Runtime.Loader;
using Distributed.Library;
using System.Diagnostics;
using System.IO;

using Distributed.Assembly;
/// <summary>
/// Loader test app, this will be migrated into 
/// </summary>
namespace Loader
{
    /// <summary>
    /// Class to load a user program in and run it.
    /// </summary>
    public class Loader
    {
        /// <summary>
        /// Loads and runs the users program
        /// </summary>
        /// <param name="args">[0] is the DLL path, 
        /// [1] is any library args separated by ','
        /// The rest are user arguments to their program</param>
        public static void Main(string[] args)
        {
            // Create a place for user arguments
            string[] user_args = new string[args.Length - 2];
            Console.WriteLine("Arguments: ");
            for (int i = 0; i < args.Length - 2; i++)
            {
                Console.WriteLine(args[i+2]);
                user_args[i] = args[i + 2];
            }
            string pwd = Directory.GetCurrentDirectory();
            // parse the arguments needed for proper execution
            int[] lib_args = ParseTokens(args);

            Console.WriteLine(pwd + "\\" + args[0]);
            // Create an assembly loader object for the users program
            CoreLoader coreLoader = new CoreLoader(pwd + "\\" + args[0]);
            // Find the Job Class, this must exist to run distributed.
            bool foundJobClass = coreLoader.FindJobClass();
            if (foundJobClass)
            {
                // if they inherited the Job class it will have Main
                // call it with the user arguments. Nothing
                // is exepected back from Main but an object result is returned
                // from the call method function.
                var result = coreLoader.CallMethod("Main", new object[] { user_args });
                // This method does not require arguments, returns int
                // calls Count on the internal task list being kept by the job.
                int numTasks = (int) coreLoader.CallMethod("GetNumberTasks", new object[] { });
                // schedule does not take any arguments
                Schedule s = (Schedule) coreLoader.CallMethod("GetSchedule", new object[] { });
                switch(s)
                {
                    case Schedule.FIXED:
                        RunFixedSchedule(coreLoader, numTasks, lib_args);
                        break;
                    default:
                        Console.WriteLine("Default called..." + s);
                        break;
                }
            }
            else
            {
                // TODO this DLL is not valid, maybe try
                // running static main to see what happens?
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
            int[] result = new int[3];
            // split on comma
            string[] s = args[1].Split(',');
            foreach(string so in s)
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
        public static void RunFixedSchedule(CoreLoader coreLoader, int numTasks, int[] libArgs) 
        {
            int fixed_block = numTasks / libArgs[2];
            Console.WriteLine("FixedBlock = " + fixed_block);
            int start = fixed_block * libArgs[1];
            Console.WriteLine("Start = " + start);
            int end = start + fixed_block;
            if (end > numTasks)
            {
                end = numTasks;
            }
            Console.WriteLine("End = " + end);
            for (int i = start; i < end; i++)
            {
                Console.WriteLine("Trying to start a task");
                coreLoader.CallMethod("startTask", new object[] { i });
            }
        }
    }
}
