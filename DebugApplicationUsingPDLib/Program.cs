using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Distributed.Node;
using Distributed.Proxy;
namespace DebugApplicationUsingPDLib
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
                NodeManager.Main();
            else
            {
                NodeSender n = new NodeSender();
                
                Proxy p = new Proxy(new NodeReceiver(), n, "127.0.0.1", 12345);
                n.Start();
            }
        }
    }
}
