using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Distributed.Proxy;

namespace Distributed
{
    class Query
    {
    }

    internal class QuerySender : AbstractSender
    {
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }
    }

    internal class QueryReceiver : AbstractReceiver
    {
        public override void Run()
        {
            throw new NotImplementedException();
        }
    }
}
