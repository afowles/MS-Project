using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Defcore.Distributed.Jobs;
using ImageSharp;
using ImageSharp.PixelFormats;

namespace Buddhabrot
{
    public class BuddhabrotDis : Job
    {
        private const int DesiredNodes = 1;

        private const int Width = 3160; private const int Height = 4840;
        private const int Iter = 5000; private const double Xmin = -1.5;
        private const double Xmax = 1.1; private const int Samples = 200000000;

        public override int RequestedNodes()
        {
            return DesiredNodes;
        }
        
        public override void Main(string[] args)
        {
            for (var i = 0; i < DesiredNodes; i++)
            {
                AddTask(new BuddhabrotTask(Width, Height, Xmin, Xmax, Iter, Samples));
            }
        }

        public override void RunFinalTask()
        {
            var totalMatrix = new double[Width * Height];
            foreach (var jobResult in Results)
            {
                var r = jobResult as BuddhabrotResult;
                if (r == null) { return;}
                Parallel.For(0, r.Data[0].Length, i => { totalMatrix[i] = r.Data.Sum(x => x[i]); });
            }
            
            var limit = Brighten(totalMatrix);
            // ImageSharp Lib for .NET core images
            var img = new Image(Width, Height);
            var pixels = img.Lock();
            for (var y = 0; y < Height; y++)
            {
                Parallel.For(0, Width, x =>
                {
                    var val = totalMatrix[x + y * Width] / limit * 256;
                    if (val > 255) { val = 255; }
                    // ImageSharp lib
                    pixels[x, y] = new Rgba32((byte)(val), (byte)(val), (byte)(val));
                });
            }

            img.Save("crazy.jpg");
        }

        private static double Brighten(double[] totalMatrix, double sammpleThreshold = 0.01, double threshold = 0.9995)
        {
            var rand = new Random();
            var sampleCount = (int)(totalMatrix.Length * sammpleThreshold);

            if (sampleCount < 1000) { sampleCount = 1000; }
            if (sampleCount > totalMatrix.Length){ sampleCount = totalMatrix.Length;}

            var samples = Enumerable.Range(0, sampleCount).Select(x => totalMatrix[rand.Next(0, totalMatrix.Length)])
                .OrderBy(x => x)
                .ToArray();

            var sampleThreshold = samples[(int)(samples.Length * 0.98)];
            var brightSamples = totalMatrix.Where(sample => sample > sampleThreshold).ToList();

            var values = brightSamples.OrderBy(x => x).ToArray();
            var k = values.Length - (int)(values.Length * threshold);
            var limit = values[values.Length - k - 1];
            return limit;
        }
    }

    public class BuddhabrotTask : JobTask
    {
        
        private long _total; // total number of iterations before stopping
        private readonly int _threadCount, _width, _iterations;
        private readonly double[][] _data;
        private readonly double _xMin, _yMin, _xMax, _yMax, _nx, _ny;
        private readonly long _samples;


        public BuddhabrotTask(int width, int height, double xMin, 
            double xMax, int iterations, long maxSamples)
        {
            // use the processor count on the node for number of threads
            _threadCount = Environment.ProcessorCount;
            _width = width; _iterations = iterations; _xMin = xMin;
            _xMax = xMax; _samples = maxSamples; _total = 0L;

            var aspectRatio = width / (double)height;
            var xSize = xMax - xMin;
            var ySize = xSize / aspectRatio;
            _yMin = -ySize / 2; _yMax = ySize / 2;
            _data = new double[_threadCount][];
            for (var i = 0; i < _threadCount; i++)
            {
                _data[i] = new double[width * height];
            }
            _nx = 1 / xSize * width;
            _ny = 1 / ySize * height;
        }

        public override void Main(string[] args)
        {
            var tasks = Enumerable.Range(0, _threadCount)
                .Select(threadId => Task.Run(() => Run( _data[threadId], threadId)))
                .ToArray();

            Console.WriteLine(tasks.Length);
            Task.WaitAll(tasks);
            AddResult(new BuddhabrotResult { Data = _data });
        }

        private void Run(IList<double> array, int threadId)
        {
            var r = new Random(threadId);
            long currentCount = 0;
            while (true)
            {
                // calculate the random points
                var x = r.NextDouble() * 2 * (_xMax - _xMin) + _xMin;
                var y = r.NextDouble() * 2 * (_yMax - _yMin) + _yMin;

                double zr = 0.0, zi = 0.0, cr = x, ci = y;
                var escape = false;

                // calculate if we have escaped
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
                    zr = 0; zi = 0;
                    for (var i = 0; i < _iterations; i++)
                    {
                        var zzr = zr * zr - zi * zi;
                        var zzi = zr * zi + zi * zr;
                        zr = zzr + cr;
                        zi = zzi + ci;

                        if ((zr * zr + zi * zi) > 14)
                            break;

                        Increment(array, zr, zi);
                        Increment(array, zr, -zi);
                    }
                }

                currentCount++;

                if (currentCount % 10000000 != 0) continue;
                
                // increase the total for all threads sharing this
                Interlocked.Add(ref _total, currentCount);
                Console.WriteLine("Calculated {0:0.0} Million samples", _total / 1000000.0);
                currentCount = 0;

                if (_total >= _samples)
                {
                    return;
                }
            }
        }

        private void Increment(IList<double> arr, double x, double y)
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
    }

    /// <summary>
    ///  Store our result
    /// </summary>
    public class BuddhabrotResult : JobResult
    {
        public double[][] Data { get; set; }
    }
}
