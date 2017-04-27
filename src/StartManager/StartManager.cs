
using System;
using Defcore.Distributed.Jobs;
using Defcore.Distributed.Manager;

namespace StartManager
{
    public class StartManager
    {
        public static void Main(string[] args)
        {
            NodeManager.Main();
        }
    }

    public class Test : JobResult
    {
        public string s = "works";

        public Test(int id) : 
            base()
        {
        }
    }
}
