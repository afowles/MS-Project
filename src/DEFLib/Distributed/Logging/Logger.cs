using System;
using System.IO;

namespace Defcore.Distributed.Logging
{
    /// <summary>
    /// Class to handle logging, it is either a logger
    /// for a manager or for a node.
    /// </summary>
    public sealed class Logger
    {
        // instance variable, volatile to make sure that 
        // assignment completes before using the instance
        private static volatile Logger _instance;
        // lock for the instance
        private static readonly object LoggerLock = new object();
        private readonly TextWriter _writer;

        /// <summary>
        /// Log output file name
        /// </summary>
        const string NODE_LOG = "node_log.txt";
        /// <summary>
        /// Log output file name
        /// </summary>
        const string MANAGER_LOG = "manager_log.txt";

        /// <summary>
        /// Logger is a Singleton
        /// </summary>
        private Logger(LogType t)
        {
            _writer = File.AppendText(t == LogType.MANAGER ? MANAGER_LOG : NODE_LOG);
            StartLogging(t);

        }

        /// <summary>
        /// Type of log
        /// </summary>
        public enum LogType
        {
            NODE,
            MANAGER
        }

        /// <summary>
        /// Accesser for the logger instance
        /// </summary>
        /// <remarks>
        /// Whichever property gets called first
        /// is what the Logger instance will be.
        /// since a Node and NodeManager are not running
        /// the same instance this will not be an issue.
        /// </remarks>
        public static Logger NodeLogInstance
        {
            get
            {
                if (_instance == null)
                {
                    // thread safety is instance
                    // is not yet initalized.
                    lock (LoggerLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger(LogType.NODE);
                        }
                    }
                }

                return _instance;
            }     
        }

        /// <summary>
        /// Accesser for the Manager logger instance
        /// </summary>
        /// <remarks>
        /// Whichever property gets called first
        /// is what the Logger instance will be.
        /// since a Node and NodeManager are not running
        /// the same instance this will not be an issue.
        /// </remarks>
        public static Logger ManagerLogInstance
        {
            get
            {
                if (_instance == null)
                {
                    // thread safety is instance
                    // is not yet initalized.
                    lock (LoggerLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger(LogType.MANAGER);
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Start Logging
        /// </summary>
        private void StartLogging(LogType t)
        {
            
            _writer.Write("Start Log for {0}: ", t.ToString());
            _writer.WriteLine("{0}", DateTime.Now.ToUniversalTime());
            _writer.WriteLine("-------------------------------");
            _writer.Flush();
        }

        /// <summary>
        /// Log a message to the text file
        /// </summary>
        /// <remarks>
        /// Print to console if in debug
        /// </remarks>
        /// <param name="logMessage"></param>
        public void Log(string logMessage)
        {

#if DEBUG
Console.WriteLine(logMessage);
#endif
            _writer.Write("\r\nLog Entry : ");
            _writer.WriteLine("{0}", DateTime.Now.ToUniversalTime());
            //Writer.WriteLine("  :");
            _writer.WriteLine("  :{0}", logMessage);
            _writer.WriteLine("-------------------------------");
            _writer.Flush();
        }

    }
}
