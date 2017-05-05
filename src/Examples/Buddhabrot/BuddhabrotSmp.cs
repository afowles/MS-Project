using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageSharp;
using ImageSharp.PixelFormats;

namespace Buddhabrot
{
    public class BuddhabrotSmp
    {

        /// <summary>
        /// Main for the BuddhabrotSmp
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            const int width = 3160;
            const int height = 4840;
            const int iter = 1000;
            const double xmin = -1.5;
            const double xmax = 1.1;
            const int samples = 40000000;

            double[][] arrays = null;
            var brot = new BuddhabrotSmpAlgo(width, height, xmin, xmax, iter, samples);
            var process = Task.Run(() => arrays = brot.Run());
            process.Wait();

            SaveImage("demosmp.jpg", width, height, arrays);
            

        }

        /// <summary>
        /// Save the image
        /// </summary>
        /// <param name="filename">image filename</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="arrays">arrays of data</param>
        public static void SaveImage(string filename, int width, int height, double[][] arrays)
        {
            var totalMatrix = new double[width * height];
            Parallel.For(0, arrays[0].Length, i => { totalMatrix[i] = arrays.Sum(x => x[i]); });
            // Used to normalize the intensity of the pixels
            var limit = Brighten(totalMatrix);
     
            //var imageData = new byte[width * height * ch];
            var img = new Image(width, height);
            var pixels = img.Lock();
            for (var y = 0; y < height; y++)
            {
                Parallel.For(0, width, x =>
                {
                    var val = totalMatrix[x + y * width] / limit * 256;
                    if (val > 255) { val = 255; }
                        

                    //pixels[x, y] = new Rgba32((byte)(val), (byte)(val), (byte)(val));
                    pixels[x, y] = new Rgba32(0, (byte)(val), (byte)(val));
                });
            }

            img.Save(filename);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalMatrix"></param>
        /// <param name="sammpleThreshold"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private static double Brighten(double[] totalMatrix, 
            double sammpleThreshold = 0.01, double threshold = 0.9995)
        {
            var rand = new Random();
            var sampleCount = (int)(totalMatrix.Length * sammpleThreshold);

            if (sampleCount < 1000)
                sampleCount = 1000;
            if (sampleCount > totalMatrix.Length)
                sampleCount = totalMatrix.Length;

            var samples = Enumerable.Range(0, sampleCount)
                .Select(x => totalMatrix[rand.Next(0, totalMatrix.Length)])
                .OrderBy(x => x)
                .ToArray();

            // pull out the 20% brightest pixels
            var sampleThreshold = samples[(int)(samples.Length * 0.80)];
            var brightSamples = totalMatrix.Where(sample => sample > sampleThreshold).ToList();
            var values = brightSamples.OrderBy(x => x).ToArray();
            var k = values.Length - (int)(values.Length * threshold);
            var limit = values[values.Length - k - 1];
            return limit;
        }
    }

/*
Testing Distributed 
BuddhabrotDis d = new BuddhabrotDis();
d.Main(args);
d.StartTask(0);
Console.WriteLine("Finished First");
d.StartTask(1);
Console.WriteLine("Finished Second");
d.StartTask(2);
Console.WriteLine("Finished Third");
d.StartTask(3);
Console.WriteLine("Finished Four");
d.CompileResults();
d.RunFinalTask();
*/
}