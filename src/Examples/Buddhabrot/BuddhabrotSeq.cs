using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharp;
using ImageSharp.PixelFormats;

namespace Buddhabrot
{
    /// <summary>
    /// Class to compute Buddhabrot Seq
    /// </summary>
    public class BuddhabrotSeq
    {

        private readonly int  _width, _iterations;
        private readonly long _samples;
        private readonly double _xMin, _yMin, _xMax, _yMax, _nx, _ny;
        private readonly double[] _data;

        private readonly Random _random;

        /// <summary>
        /// Construct the BuddbrotSeq program
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="xMin">min x range</param>
        /// <param name="xMax">min y range</param>
        /// <param name="iterations">number of iterations</param>
        /// <param name="maxSamples">num of samples to go for</param>
        public BuddhabrotSeq(int width, int height, double xMin,
            double xMax, int iterations, long maxSamples)
        {
            var aspectRatio = width / (double)height;
            _width = width;
            _iterations = iterations;
            _xMin = xMin;
            _xMax = xMax;
            _samples = maxSamples;
            _random = new Random();
            _data = new double[width * height];

            var xSize = xMax - xMin;
            var ySize = xSize / aspectRatio;
            _yMin = -ySize / 2;
            _yMax = ySize / 2;
            _nx = 1 / xSize * width;
            _ny = 1 / ySize * height;
        }

        /// <summary>
        /// Calculate the mandelbrot randomly
        /// aka buddhabrot
        /// </summary>
        /// <returns></returns>
        public double[] CalculateMandelbrot()
        {
            long currentCount = 0;

            while (true)
            {
                // grab our random points for x and y
                var x = _random.NextDouble() * 2 * (_xMax - _xMin) + _xMin;
                var y = _random.NextDouble() * 2 * (_yMax - _yMin) + _yMin;

                double zr = 0.0, zi = 0.0, cr = x, ci = y;

                var escape = false;

                // Calculate if we have escaped
                for (var i = 0; i < _iterations; i++)
                {
                    var zzr = zr * zr - zi * zi;
                    var zzi = zr * zi + zi * zr;
                    zr = zzr + cr;
                    zi = zzi + ci;
                    escape = (zr * zr + zi * zi) > 4;
                    if (escape) { break; }
                }

                if (escape)
                {
                    // if so iterate again
                    zr = 0; zi = 0;
                    for (var i = 0; i < _iterations; i++)
                    {
                        var zzr = zr * zr - zi * zi;
                        var zzi = zr * zi + zi * zr;
                        zr = zzr + cr;
                        zi = zzi + ci;

                        if ((zr * zr + zi * zi) > 14)
                            break;

                        // increase the count in the data array
                        // the higher the count the brighter
                        IncreasePixel(_data, zr, zi);
                        IncreasePixel(_data, zr, -zi);
                    }
                }

                currentCount++;

                // are we done?
                if (currentCount >= _samples)
                {
                    return _data;
                }
            }
        }

        /// <summary>
        /// Increase the pixel in the matrix
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void IncreasePixel(double[] arr, double x, double y)
        {
            // don't increase if not in range
            if (x >= _xMax || x < _xMin) { return; }
            if (y >= _yMax || y < _yMin) { return; }
            // find the pixel
            var nx = (int)((x - _xMin) * _nx);
            var ny = (int)((y - _yMin) * _ny);
            var idx = nx + ny * _width;
            arr[idx]++;
        }

        /// <summary>
        /// Main program for BuddhabrotSeq
        /// </summary>
        /// <param name="args"></param>
        public static void Main2(string[] args)
        {
            // setup variables
            const int width = 500;
            const int height = 500;
            const int iter = 1000;
            const double xmin = -1.5;
            const double xmax = 1.1;
            const int samples = 10000000;

            var bb = new BuddhabrotSeq(width, height, xmin, xmax, iter, samples);
            var pixelMatrix = bb.CalculateMandelbrot();

            SaveImage("DemoSeq.jpg", width, height, pixelMatrix);

        }

        /// <summary>
        /// Save our image using ImageSharp
        /// </summary>
        /// <param name="filename">filename of image </param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="data">image data</param>
        public static void SaveImage(string filename, int width, int height, double[] data)
        {
            
            // Used to normalize the intensity of the pixels
            var limit = Brighten(data);

            //var imageData = new byte[width * height * ch];
            var img = new Image(width, height);
            var pixels = img.Lock();
            for (var y = 0; y < height; y++)
            {
                for(var x = 0; x < width; x++) 
                {
                    var val = data[x + y * width] / limit * 256;
                    // stay within range
                    if (val > 255) { val = 255; }

                    // add pixels to our image, using ImageSharp nuget package
                    // for creating and saving images
                    pixels[x, y] = new Rgba32(0, (byte)(val), 0);
                }
            }

            img.Save(filename);

        }

        /// <summary>
        /// Brighten by pulling out certain pixels
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="sammpleThreshold"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private static double Brighten(double[] matrix, 
            double sammpleThreshold = 0.01, double threshold = 0.9995)
        {
            var rand = new Random();
            var sampleCount = (int)(matrix.Length * sammpleThreshold);

            if (sampleCount < 1000)
            {
                sampleCount = 1000;
            }

            if (sampleCount > matrix.Length)
            {
                sampleCount = matrix.Length;
            }

            var samples = Enumerable.Range(0, sampleCount)
                .Select(x => matrix[rand.Next(0, matrix.Length)])
                .OrderBy(x => x)
                .ToArray();

            // pull out the 20% brightest pixels
            var sampleThreshold = samples[(int)(samples.Length * 0.80)];
            var brightSamples = matrix.Where(sample => sample > sampleThreshold).ToList();
            var values = brightSamples.OrderBy(x => x).ToArray();
            var k = values.Length - (int)(values.Length * threshold);
            var limit = values[values.Length - k - 1];

            return limit;
        }
    }
}
