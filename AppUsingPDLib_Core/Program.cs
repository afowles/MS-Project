using System;
using System.Diagnostics;

using Distributed;
using MyFSharpLibrary;

namespace DebugApplicationUsingPDLib
{
    public class TestAppJob : Job
    {
        public TestAppJob()
        {

        }

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
        public TestAppJobTask()
        {

        }

        public override void Main(string[] args)
        {
            Console.WriteLine("Completing Job: " + args[0] + "\n");
        }
    }

    public class test
    {
        public static void Main(string[] args)
        {
            var test = new User("Mark", 10);
            //Console.WriteLine(test::name);
            Console.ReadKey();
        }
    }

}
