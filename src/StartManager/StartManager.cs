
using System;
using Distributed.Library;
using Distributed.Manager;

namespace StartManager
{
    public class StartManager
    {
        public static void Main(string[] args)
        {
            //Test t1 = new Test(20);
            //Console.WriteLine(t1.SerializeResult());
            //Console.ReadKey();

            NodeManager.Main();
        }
    }

    public class Test : JobResult
    {
        public string s = "works";

        public Test(int id) : 
            base(id)
        {
        }
    }
}
