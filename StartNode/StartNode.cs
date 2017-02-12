using Distributed.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StartNode
{
    public class StartNode
    {
        public static void Main(string[] args)
        {
            Node.Main(new string[] { "129.21.89.207", "12345" });
        }
    }
}
