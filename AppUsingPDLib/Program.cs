using System;
using Distributed.Node;
using Distributed.Network;
using System.Diagnostics;

using Distributed;

namespace DebugApplicationUsingPDLib
{
    public class TestAppJob : Job
    {
        
        public override void Main(string[] args)
        {
            for (int i = 0; i < 4; i++)
            {
                AddTask(new TestAppJobTask());
            }
            
        }

    }

    public class TestAppJobTask : JobTask
    {
        public override void Main(string[] args)
        {
            Console.WriteLine("Completing Job: " + args[0]);
        }
    }

    class Program2
    {

        /// <summary>
        /// Just a simple test app, run program with some number of arguments
        /// first to start up the "server" and then run the secondary proxy
        /// after to communicate. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            Console.WriteLine("Hello World");
            bool val = false;
            if (val)
            {
                Process myProcess = new Process();

                try
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.FileName = "";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            
            
            
        }
        public delegate void MainMethod();
    }
}
