using System;
using System.IO;
using System.Net.Sockets;

namespace Defcore.Distributed.IO
{
    /// <summary>
    /// Static class to handle File Reading
    /// </summary>
    public static class FileRead
    {
        public const int BufferSize = 1024;
        public const int NetworkSleep = 100;
        public const int ReadTimeout = 250;

        /// <summary>
        /// Read in a file from a Network Stream
        /// and write that file out.
        /// </summary>
        /// <param name="iostream">Stream to read in from</param>
        /// <param name="filepath">path to write the resulting file</param>
        public static void ReadInWriteOut(NetworkStream iostream, string filepath)
        {
            // create a buffer for the incoming data
            var buff = new byte[BufferSize];
            // create a filestream for the new file
            var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            // wait for data
            while (!iostream.DataAvailable)
            {
                //Thread.Sleep(NetworkSleep);
            }

            // set a read timeout so we don't sit here forever
            iostream.ReadTimeout = ReadTimeout;
            try
            {
                int byteSize;
                while ((byteSize = iostream.Read(buff, 0, buff.Length)) > 0)
                {
                    fs.Write(buff, 0, byteSize);
                }
            }
            // TODO handle the proper exception only, rethrow any other
            catch (IOException)
            {
                Console.WriteLine("Read finished");
            }
            fs.Dispose();
            iostream.Flush();
        }
    }

    /// <summary>
    /// Static class to handle File Writing
    /// </summary>
    public static class FileWrite
    {
        /// <summary>
        /// Write a file out to the given network stream
        /// </summary>
        /// <param name="iostream">the network stream to write to</param>
        /// <param name="filepath">the file to be written</param>
        public static void WriteOut(NetworkStream iostream, string filepath)
        {
            using (var fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var binFile = new BinaryReader(fs))
                {
                    int bytesSize;
                    var downBuffer = new byte[2048];
                    while ((bytesSize = binFile.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        iostream.Write(downBuffer, 0, bytesSize);
                    }
                }
            }
            iostream.Flush();
        }
    }

}
