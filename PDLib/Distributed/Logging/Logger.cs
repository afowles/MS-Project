using System;
using System.IO;

namespace Distributed.Logging
{
    /// <summary>
    /// Class to handle logging, it is either a logger
    /// for a manager or for a node.
    /// </summary>
    public sealed class Logger
    {
        // instance variable, volatile to make sure that 
        // assignment completes before using the instance
        private static volatile Logger instance;
        // lock for the instance
        private static object LoggerLock = new Object();
        private TextWriter Writer;

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
            if (t == LogType.MANAGER)
            {
                Writer = File.AppendText(MANAGER_LOG);
            }
            else
            {
                Writer = File.AppendText(NODE_LOG);
            }
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
                if (instance == null)
                {
                    // thread safety is instance
                    // is not yet initalized.
                    lock (LoggerLock)
                    {
                        if (instance == null)
                        {
                            instance = new Logger(LogType.NODE);
                        }
                    }
                }

                return instance;
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
                if (instance == null)
                {
                    // thread safety is instance
                    // is not yet initalized.
                    lock (LoggerLock)
                    {
                        if (instance == null)
                        {
                            instance = new Logger(LogType.MANAGER);
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Start Logging
        /// </summary>
        private void StartLogging(LogType t)
        {
            
            Writer.Write("Start Log for {0}: ", t.ToString());
            Writer.WriteLine("{0}", DateTime.Now.ToUniversalTime());
            Writer.WriteLine("-------------------------------");
            Writer.Flush();
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
            Writer.Write("\r\nLog Entry : ");
            Writer.WriteLine("{0}", DateTime.Now.ToUniversalTime());
            //Writer.WriteLine("  :");
            Writer.WriteLine("  :{0}", logMessage);
            Writer.WriteLine("-------------------------------");
            Writer.Flush();
        }

    }
}
