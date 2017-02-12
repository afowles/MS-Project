using System;
using System.Net;
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

        /// <summary>
        /// A Proxy constructor that can be passed
        /// an already initalized TcpClient.
        /// </summary>
        /// <param name="r">An Abstract Receiver</param>
        /// <param name="s">An Abstract Sender</param>
        /// <param name="client">An initalized</param>
        public Proxy(AbstractReceiver r, AbstractSender s, TcpClient client)
        {
            this.client = client;
            
            iostream = client.GetStream();
        
            receiver = r;
            sender = s;
            ConfigureSenderReceiver();
        }

        /// <summary>
        /// A Proxy constructor that takes in the
        /// string and port name to create a Tcp connection
        /// </summary>
        /// <param name="r">An Abstract Receiver</param>
        /// <param name="s">An Abstract Sender</param>
        /// <param name="host">host name for tcp</param>
        /// <param name="port">port number for tcp</param>
        public Proxy(AbstractReceiver r, AbstractSender s, string host, int port)
        {
            try
            {
                // this is not supported for .NET core...
                //this.client = new TcpClient(host, port);
                client = new TcpClient();
                Socket sock = client.Client;
                IPAddress ipAddress = IPAddress.Parse(host);
                client.ConnectAsync(ipAddress, port);
                
                // should probably use await...
                while (!client.Connected) { }
            }
            catch(SocketException e)
            {
                Console.WriteLine(e);
                // should stop or throw exception
                // without the socket no communication
                // will happen
            }
            this.iostream = client.GetStream();
            this.receiver = r;
            this.sender = s;
            ConfigureSenderReceiver();  
        }

        /// <summary>
        /// Configures the sender and receiver
        /// objects correctly for Proxy constructors.
        /// </summary>
        public void ConfigureSenderReceiver()
        {
            // allow sending of messages quickly
            client.NoDelay = true;
            // add ourselves to the sender and receiver
            // so that they can grab the TcpInterface
            // and use proxy methods.
            receiver.proxy = this;
            sender.proxy = this;
            // add sender to list of listeners for a data receive event
            receiver.DataReceived += sender.HandleReceiverEvent;
            // start receiving
            receiver.Start();
            // start sender
            sender.Start();
        }

        /// <summary>
        /// Shutdown the TcpClient
        /// and the underlying tcp connection.
        /// </summary>
        public void Shutdown()
        {
            // alright can't close in .NET core
            // TODO, figure something else out
            //client.Close();
        }


    }

    
    /// <summary>
    /// Event arguments class to hold info about the data received.
    /// </summary>
    /// <remarks>This should be subclassed depending
    /// on what kind of data is being received</remarks>
    public abstract class DataReceivedEventArgs : EventArgs
    {
        private string data;

        public DataReceivedEventArgs(string msg)
        {
            data = msg;
        }

        public string message
        {
            get { return data; }
        }

    }

    /// <summary>
    /// Provides the methods
    /// nessesary to be the receiving side of a proxy
    /// object. 
    /// </summary>
    public abstract class AbstractReceiver : NetworkSendReceive
    {
        /// <summary>
        /// The event for receiving data.
        /// </summary>
        /// <remarks>To use this without a delegate
        /// the DataReceivedEventArgs class was created. </remarks>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Raises the data received event.
        /// </summary>
        /// <param name="e">The data received</param>
        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Provides the methods nessesary to
    /// be the sending side of a proxy object
    /// </summary>
    public abstract class AbstractSender : NetworkSendReceive
    {
        public abstract void HandleReceiverEvent(object sender, DataReceivedEventArgs e);
    }

    /// <summary>
    /// Provides the basic methods and members 
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
