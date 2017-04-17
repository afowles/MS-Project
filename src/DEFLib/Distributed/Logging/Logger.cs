using System;
using System.IO;

namespace Defcore.Distributed.Logging
{
    /// <summary>
    /// Class to handle logging, Logger is a singleton
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
        /// Name of the log file
        /// </summary>
        /// <remarks>Only one instance can be used per program</remarks>
        public static string LogFileName = "log.txt";

        /// <summary>
        /// Who to log for, can be used once per logger creation
        /// </summary>
        /// <remarks>Used at start and not after</remarks>
        public static string LogFor = "";

        /// <summary>
        /// Logger is a Singleton
        /// </summary>
        private Logger()
        {
            _writer = File.AppendText(LogFileName);
            StartLogging(LogFor);

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
                if (_instance != null) {return _instance;}
                // thread safety is instance
                // is not yet initalized.
                lock (LoggerLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger();
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
                if (_instance != null) {return _instance;}
                // thread safety is instance
                // is not yet initalized.
                lock (LoggerLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Start Logging
        /// </summary>
        private void StartLogging(string logFor)
        {
            
            _writer.Write("Start Logger for {0}: ", logFor);
            _writer.WriteLine("{0}", DateTime.Now.ToUniversalTime());
            _writer.WriteLine("-------------------------------");
            _writer.Flush();
        }

        /// <summary>
        /// Logger a message to the text file
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
