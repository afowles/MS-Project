using System;
using Defcore.Distributed.Jobs;

namespace DemoApp
{
    public class Class1 : Job
    {
        
        public override void Main(string[] args)
        {
            int x = int.Parse(args[0]);
            int y = int.Parse(args[0]);

            AddTask(new MyTask(x,y));
            AddTask(new MyTask(x + 5, y - 2));
        }

        public override int RequestedNodes()
        {
            return 2;
        }

        public override void RunFinalTask()
        {
            Console.WriteLine("Result");
            var sum = 0;
            foreach (var jobResult in Results)
            {
                var t = jobResult as MyResult;
                if (t == null) return;
                Console.WriteLine("result: " + t.IntResult);
                sum += t.IntResult;
            }
            Console.WriteLine("Sum is: " + sum);
        }
    }

    public class MyTask : JobTask
    {
        private int _x;
        private int _y;
           
        public MyTask(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public override void Main(string[] args)
        {
            var result = new MyResult {IntResult = _x + _y};

            AddResult(result);
        }
    }

    public class MyResult : JobResult
    {
        public int IntResult { get; set; }
    }
}
