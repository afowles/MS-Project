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
        /// <summary>
        /// Just a simple test app, run program with some number of arguments
        /// first to start up the "server" and then run the secondary proxy
        /// after to communicate. 
        /// </summary>
        /// <param name="args"></param>
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
