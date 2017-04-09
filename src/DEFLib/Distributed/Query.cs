using System;
using Defcore.Distributed.Network;

namespace Defcore.Distributed
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
        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            throw new NotImplementedException();
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }
    }
}
