using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace PD.Proxy
{
    public class Proxy
    {
        private TcpClient client;
        private NetworkStream iostream;
        private Receiver receiver;
        private Sender sender;

        public Proxy(Receiver r, Sender s, string host, int port)
        {
            this.receiver = r;
            this.sender = s;
            client = new TcpClient(host,port);
            iostream = client.GetStream();
            // allow sending of messages quickly
            client.NoDelay = true;
            // start receiving
            receiver.Start();
        }

        public Proxy()
        {
            sender = new NodeSender();
        }

        private sealed class NodeReceiver : Receiver
        {
            public override void SendData(byte[] data)
            {
                
            }

            public override void Start()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class NodeSender : Sender
        {

        }

    }

    public abstract class Receiver
    {
        protected Thread thread;
        public abstract void SendData(byte[] data);
        public abstract void Start();

    }

    public abstract class Sender
    {

    }

    



}
