using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Distributed.Network
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
        public ConnectionType connection { get; set; }
        private AbstractReceiver receiver;
        private AbstractSender sender;
        public int id { get; private set; }

        /// <summary>
        /// A Proxy constructor that can be passed
        /// an already initalized TcpClient.
        /// </summary>
        /// <param name="r">An Abstract Receiver</param>
        /// <param name="s">An Abstract Sender</param>
        /// <param name="client">An initalized TcpClient</param>
        public Proxy(AbstractReceiver r, AbstractSender s, 
            TcpClient client, int id)
        {
            this.client = client;
            this.id = id;
            iostream = client.GetStream();
            receiver = r;
            sender = s;
            ConfigureSenderReceiver();
        }

        /// <summary>
        /// A Proxy constructor that takes in the
        /// string and port name to create a Tcp connection.
        /// </summary>
        /// <param name="r">An Abstract Receiver</param>
        /// <param name="s">An Abstract Sender</param>
        /// <param name="host">host name for tcp</param>
        /// <param name="port">port number for tcp</param>
        public Proxy(AbstractReceiver r, AbstractSender s, 
            string host, int port, int id)
        {
            try
            {
                // this is not supported for .NET core...
                // this.client = new TcpClient(host, port);
                client = new TcpClient();
                this.id = id;
                Socket sock = client.Client;
                IPAddress ipAddress = IPAddress.Parse(host);
                client.ConnectAsync(ipAddress, port);
                
                // wait until the client is connected.
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
        private void ConfigureSenderReceiver()
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
            // add the receivers abstract method for additional handling
            receiver.DataReceived += receiver.HandleAdditionalReceiving;
            // start receiving
            receiver.Start();
            // start sender
            sender.Start();
            
        }

        /// <summary>
        /// Hand off the sender and receiver to a new
        /// sender and receiver object.
        /// </summary>
        /// <param name="r">new receiver</param>
        /// <param name="s">new sender</param>
        public void HandOffSendReceive(AbstractReceiver r, 
            AbstractSender s, ConnectionType c)
        {
            receiver = r;
            sender = s;
            connection = c;
            // reconfigure
            ConfigureSenderReceiver();
        }

        /// <summary>
        /// Shutdown the TcpClient
        /// and the underlying tcp connection.
        /// </summary>
        public void Shutdown()
        {
            // Can't call close in .NET core
            // client.Close();
            iostream.Dispose();   
        }

        /// <summary>
        /// Join on both send and
        /// receive threads.
        /// </summary>
        /// <remarks>
        /// Nicely wait for both threads in the proxy
        /// to finish. This is used for clean up.
        /// </remarks>
        public void Join()
        {
            receiver.Join();
            sender.Join();
        }

        /// <summary>
        /// Queue a data event for the receiver
        /// without receiving it directly from the socket.
        /// </summary>
        /// <param name="d">the data to queue</param>
        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            receiver.OnDataReceived(d);
        }

        /// <summary>
        /// Override equals for Proxy
        /// two proxy objects are equal if
        /// their id's are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as Proxy;

            if (other == null)
            {
                return false;
            }

            return this.id == other.id;
        }

        /// <summary>
        /// Override hashcode for Proxy
        /// </summary>
        /// <remarks>
        /// two objects must hash to the same
        /// thing if they are equal.
        /// </remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }
    }

    /// <summary>
    /// The type connected to the proxy
    /// </summary>
    public enum ConnectionType
    {
        DEFAULT,
        NODE,
        JOB,
        QUERY
    }

    /// <summary>
    /// Event arguments class to hold info about the data received.
    /// </summary>
    /// <remarks>This should be subclassed depending
    /// on what kind of data is being received.</remarks>
    public abstract class DataReceivedEventArgs : EventArgs
    {
        // End of a message
        public const string endl = "end*";
        public const char split = '|';
        private string data;
        public string[] args { get; }

        public DataReceivedEventArgs(string msg)
        {
            data = msg;
            args = ParseMessage(msg);
        }

        public string message
        {
            get { return data; }
        }

        /// <summary>
        /// Split incoming data on space.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected static string[] ParseMessage(string message)
        {
            return message.Split(new char[] { split });
        }

        /// <summary>
        /// From a list of string arguments
        /// construct a message to pass along.
        /// </summary>
        /// <param name="args">arguments</param>
        /// <returns></returns>
        public static string ConstructMessage(string[] args)
        {
            string message = "";
            foreach (string s in args)
            {
                message += s + split;
            }
            message += endl;
            return message;

        }

        /// <summary>
        /// Overloaded ConstructMessage for a single string
        /// </summary>
        /// <param name="arg">the argument to pass</param>
        /// <returns>a proper constructed message</returns>
        public static string ConstructMessage(string arg)
        {
            return arg + split + endl;
        }
    }

    /// <summary>
    /// Provides the methods
    /// nessesary to be the receiving side of a proxy
    /// object. 
    /// </summary>
    public abstract class AbstractReceiver : NetworkSendReceive
    {
        protected bool DoneReceiving = false;

        protected string Message = "";

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
        public virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Method that does the actual receiving loop,
        /// reads in the data then raises an on data received
        /// event for listeners.
        /// </summary>
        public override void Run()
        {
            // Grab the network IO stream from the proxy.
            NetworkStream iostream = proxy.iostream;
            // setup a byte buffer
            byte[] bytes = new byte[BUFFER_SIZE];
            string data;
            try
            {
                while (!DoneReceiving)
                {
                    try
                    {
                        // Check if we got something
                        if (!iostream.DataAvailable)
                        {
                            // sleep a short time
                            Thread.Sleep(100);
                        }
                        else if (iostream.Read(bytes, 0, bytes.Length) > 0)
                        {
                            // byte buffer to string
                            data = System.Text.Encoding.ASCII.GetString(bytes);
                            // did the message fit into the buffer?
                            if (data.Contains(DataReceivedEventArgs.endl))
                            {
                                // if so don't use the entire buffer (lot of extra space)
                                int index = data.LastIndexOf('*');
                                // but if the entire buffer is being used all the way don't try and truncate
                                if (index > 0)
                                {
                                    data = data.Remove(index, data.Substring(index).Length);
                                }
                                // append the data to the message
                                Message += data;
                                // Raise the data received event
                                OnDataReceived(CreateDataReceivedEvent(Message));
                                // reset the message
                                Message = "";
                            }
                            else
                            {
                                // the message is larger than the buffer
                                Message += data;
                            }
                            Console.WriteLine("Received: {0}", data);
                            // Clear out buffer to prevent odd messages
                            Array.Clear(bytes, 0, bytes.Length);

                        }
                        else
                        {
                            // TODO: handle failure case
                        }

                    }
                    catch (IOException e)
                    {
                        //TODO: handle this
                        Console.Write(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Catching exception in Abstract");
                //TODO: handle this
                Console.Write(e);
            }
            finally
            {
                // if we get here from anything other than
                // a normal shutdown, close the proxy
                if (!DoneReceiving)
                {
                    proxy.Shutdown();
                }
                   
            }
        }

        /// <summary>
        /// Return a concrete DataReceivedEventArgs subclass, 
        /// for handling receive events.
        /// </summary>
        /// <remarks>
        /// A subclass must be able to
        /// create and return a data received event object
        /// for its specific needs. 
        /// </remarks>
        /// <param name="data">data from socket</param>
        /// <returns>new DataRecivedEventArgs object type</returns>
        public abstract DataReceivedEventArgs CreateDataReceivedEvent(string data);

        /// <summary>
        /// Process received data in addition to simply sending it to the sender object.
        /// </summary>
        /// <remarks>
        /// A subclass must implement any additional handling that they want done.
        /// when data is received.
        /// </remarks>
        /// <param name="sender">event raiser</param>
        /// <param name="e">data from receive event</param>
        public abstract void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e);
    }

    /// <summary>
    /// Provides the methods nessesary to
    /// be the sending side of a proxy object
    /// </summary>
    public abstract class AbstractSender : NetworkSendReceive
    {
        protected bool DoneSending = false;

        /// <summary>
        /// Handle the nessesary communication in response
        /// to data received from the socket.
        /// </summary>
        /// <param name="sender">event raiser</param>
        /// <param name="e">data from receive event</param>
        public abstract void HandleReceiverEvent(object sender, DataReceivedEventArgs e);

        /// <summary>
        /// Format and send a message from an array
        /// of string arguments.
        /// </summary>
        /// <param name="args">the message to send</param>
        public virtual void SendMessage(string[] args)
        {
            string message = "";
            foreach(string s in args)
            {
                message += s + DataReceivedEventArgs.split;
                Console.WriteLine(s);
            }
            message += DataReceivedEventArgs.endl;
            byte[] b = System.Text.Encoding.ASCII.GetBytes(message);
            proxy.iostream.Write(b, 0, b.Length);
            
        }

    }

    /// <summary>
    /// Provides the basic methods and members 
    /// nessesary for network opperations. 
    /// </summary>
    public abstract class NetworkSendReceive
    {
        // Setup constant buffer size
        public const int BUFFER_SIZE = 1024;
        public const int SERVER_PORT = 12345;
        public const int IO_SLEEP = 100; // in milliseconds
        public const int SERVER_SLEEP = 500;
        // Thread object to handle from socket.
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

        /// <summary>
        /// Join on this thread.
        /// </summary>
        public virtual void Join()
        {
            thread.Join();
        }
    }

}
