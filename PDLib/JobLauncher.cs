using System;
using Distributed.Proxy;

namespace Distributed
{
    class JobLauncher
    {
    }


    internal class JobReceiver : AbstractReceiver
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

    internal class JobSender : AbstractSender
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
}
