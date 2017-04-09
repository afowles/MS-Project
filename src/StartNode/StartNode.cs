using Defcore.Distributed.Nodes;
using System;

namespace StartNode
{
    public class StartNode
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: <ip>");
                return;
            }
            //TODO: Figure out a better way to get IP
            Node.Main(new string[] { args[0], "12345" });
        }
    }
}
