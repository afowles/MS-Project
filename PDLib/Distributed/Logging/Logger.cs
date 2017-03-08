using System;
using System.IO;

namespace Distributed.Logging
{
    /// <summary>
    /// Class to handle logging
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
        private Logger(){ }

        public enum LogType
        {
            NODE,
            MANAGER
        }

        /// <summary>
        /// Accesser for the logger instance
        /// </summary>
        public static Logger LogInstance
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
                            instance = new Logger();
                        }
                    }
                }

                return instance;
            }     
        }

        /// <summary>
        /// Start Logging
        /// </summary>
        public void StartLogging(LogType t)
        {
            if (t == LogType.MANAGER)
            {
                Writer = File.AppendText(MANAGER_LOG);
            }
            else
            {
                Writer = File.AppendText(NODE_LOG);
            }
            Writer.Write("Start Log for {0}: ", t.ToString());
            Writer.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            Writer.WriteLine("-------------------------------");
        }

        public void Log(string logMessage)
        {
            if (Writer == null)
                return;

            Writer.Write("\r\nLog Entry : ");
            Writer.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            Writer.WriteLine("  :");
            Writer.WriteLine("  :{0}", logMessage);
            Writer.WriteLine("-------------------------------");
        }

        public static void DumpLog(StreamReader r)
        {
            string line;
            while ((line = r.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }
}
