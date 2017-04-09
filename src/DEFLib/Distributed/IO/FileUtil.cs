using System;
using System.IO;
using System.Net.Sockets;

namespace Distributed.Files
{
    internal static class FileRead
    {
        public const int BufferSize = 1024;
        public const int NetworkSleep = 100;

        /// <summary>
        /// Read in a file from a Network Stream
        /// and write that file out.
        /// </summary>
        /// <param name="iostream">Stream to read in from</param>
        /// <param name="filepath">path to the file to write out to</param>
        public static void ReadInWriteOut(NetworkStream iostream, string filepath)
        {
            // create a buffer for the incoming data
            byte[] buff = new byte[BufferSize];
            int byteSize = 0;
            // create a filestream for the new file
            FileStream Fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            // wait for data
            Console.WriteLine("Waiting...");
            while (!iostream.DataAvailable)
            {
                //Thread.Sleep(NetworkSleep);
            }

            Console.WriteLine("Reading file...");
            // set a read timeout so we don't sit here forever
            iostream.ReadTimeout = 250;
            try
            {
                while ((byteSize = iostream.Read(buff, 0, buff.Length)) > 0)
                {
                    Fs.Write(buff, 0, byteSize);
                }
            }
            // TODO handle the proper exception only, rethrow any other
            catch (IOException)
            {
                Console.WriteLine("Read finished");
            }
            Fs.Dispose();
            iostream.Flush();
        }
    }

    internal static class FileWrite
    {
        public static void WriteOut(NetworkStream iostream, string filepath)
        {
            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binFile = new BinaryReader(fs))
                {
                    Console.WriteLine("IN HERE");
                    int bytesSize = 0;
                    byte[] downBuffer = new byte[2048];
                    while ((bytesSize = binFile.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        Console.WriteLine("WRITING");
                        iostream.Write(downBuffer, 0, bytesSize);
                    }
                }
            }
            iostream.Flush();
        }
    }

}
