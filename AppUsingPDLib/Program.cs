using System;
using System.Diagnostics;

using Distributed;

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
            Console.WriteLine("Completing Job: " + args[0]);
        }
    }

    }
}
