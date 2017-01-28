using System;
using System.Net.Sockets;
using System.Threading;

namespace Distributed.Proxy
{
    /// <summary>
    /// A proxy is a TcpClient wrapper
    /// that uses a sending and receiving object
    /// to do separte handling of stream input/output.
    /// </summary>
    public class Proxy
    {
        private TcpClient client;
        public NetworkStream iostream { get; private set; }

        private AbstractReceiver receiver;
        private AbstractSender sender;

        public Proxy(AbstractReceiver r, AbstractSender s, string host, int port)
        {
            this.client = new TcpClient(host, port);
            this.iostream = client.GetStream();
            this.receiver = r;
            this.sender = s;
            
            
            // allow sending of messages quickly
            client.NoDelay = true;
            // add ourselves to the sender and receiver;
            receiver.proxy = this;
            sender.proxy = this;
            // start receiving
            receiver.Start();
        }
        
        /// <summary>
        /// Shutdown the TcpClient
        /// and the underlying tcp connection.
        /// </summary>
        public void Shutdown()
        {
            client.Close();
        }


    }

    /// <summary>
    /// Abstract receiver provides the methods
    /// nessesary to be the receiving side of a proxy
    /// object. 
    /// </summary>
    /// <note>
    /// Since TcpClient provides a Network Stream
    /// as opposed to an Input or Output stream there is no difference,
    /// from an abstract point of view, between the methods of an abstract
    /// receiver and an abstract sender. The classes simply provide a readable
    /// way to differientiate the two kinds of objects. 
    /// </note>
    /// 
    public abstract class AbstractReceiver : NetworkSendReceive { }
    /// <summary>
    /// Abstract sender provides the methods nessesary to
    /// be the sending side of a proxy object
    /// </summary>
    /// <note>See abstract receiver note</note>
    public abstract class AbstractSender : NetworkSendReceive { }

    /// <summary>
    /// Provides the methods and members 
    /// nessesary to send or receive for
    /// a proxy object.
    /// </summary>
    public abstract class NetworkSendReceive
    {
        //TODO: Add some notion of what kind of sender/receiver we are
        // Thread object to handle  from
        // socket.
        protected Thread thread;
        // The proxy object that the sender/receiver is
        // sending/receiving for.
        public Proxy proxy { protected get; set; }

        /// <summary>
        /// Where the actual sending/receiving happens
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Start up the given sender/receiver in its own thread
        /// so that it can begin sending/receiving data.
        /// </summary>
        public virtual void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        /// <summary>
        /// Shutdown the connection to the socket
        /// </summary>
        public virtual void Shutdown()
        {
            proxy.Shutdown();
        }
    }

    



}
