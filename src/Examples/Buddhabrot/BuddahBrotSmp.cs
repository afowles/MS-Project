using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ImageSharp;
using ImageSharp.PixelFormats;

namespace Buddhabrot
{
    public class Brot
    {

        public static void Main(string[] args)
        {
           /*
            var width = 3160;
            var height = 4840;
            var iter = 2000;
            var xmin = -1.5;
            var xmax = 1.1;
            var samples = 1000000000;

            double[][] arrays = null;
            var brot = new BuddhabrotAlgo(width, height, xmin, xmax, iter, samples);
            var process = Task.Run(() => arrays = brot.Run());

            process.Wait();
            SaveImage("test", width, height, arrays);
            */
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

        }


        public static void SaveImage(string filename, int width, int height, double[][] arrays)
        {
            var totalMatrix = new double[width * height];
            Parallel.For(0, arrays[0].Length, i => { totalMatrix[i] = arrays.Sum(x => x[i]); });
            // Used to normalize the intensity of the pixels
            var limit = GetNormalizer(totalMatrix);
     
            //var imageData = new byte[width * height * ch];
            var img = new Image(width, height);
            var pixels = img.Lock();
            for (var y = 0; y < height; y++)
            {
                Parallel.For(0, width, x =>
                {
                    var val = totalMatrix[x + y * width] / limit * 256;
                    if (val > 255)
                        val = 255;

                    pixels[x, y] = new Rgba32((byte)(val), (byte)(val), (byte)(val));
                });
            }

            img.Save("blackwhite.jpg");

        }

        private static double GetNormalizer(double[] totalMatrix, double sammpleThreshold = 0.01, double threshold = 0.9995)
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

            // pull out the 2% brightest pixels
            var sampleThreshold = samples[(int)(samples.Length * 0.98)];
            var brightSamples = new List<double>();
            for (int i = 0; i < totalMatrix.Length; i++)
            {
                var sample = totalMatrix[i];
                if (sample > sampleThreshold)
                    brightSamples.Add(sample);
            }

            var values = brightSamples.OrderBy(x => x).ToArray();
            var k = values.Length - (int)(values.Length * threshold);
            var limit = values[values.Length - k - 1];
            return limit;
        }
    }
}