using System;
using System.IO;
using System.Net.Sockets;



namespace Distributed.Files
{
    static class FileRead
    {
        public const int BufferSize = 1024;

        public static void ReadInWriteOut(NetworkStream iostream, string filename)
        {
            // create a buffer for the incoming data
            byte[] buff = new byte[BufferSize];
            int byteSize = 0;
            // create a filestream for the new file
            FileStream Fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            // wait for data
            while (!iostream.DataAvailable) { }

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
            catch (IOException)
            {
                Console.WriteLine("Read finished");
            }
            Fs.Dispose();
            iostream.Flush();
        }
    }

}
