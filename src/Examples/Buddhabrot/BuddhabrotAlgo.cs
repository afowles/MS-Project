using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Buddhabrot
{
    internal class BuddhabrotAlgo
    {
        private long _total;

        private readonly int threadCount, width, iterations;
        private readonly double[][] _data;
        private readonly double _xMin, _yMin, _xMax, _yMax, _nx, _ny;
        private readonly long _samples;
        

        public BuddhabrotAlgo(int width, int height, double xMin, 
            double xMax, int iterations, long maxSamples)
        {
            var aspectRatio = width / (double)height;
            threadCount = Environment.ProcessorCount;
            this.width = width;
            this.iterations = iterations;
            _xMin = xMin;
            _xMax = xMax;
            _samples = maxSamples;
            _total = 0L;

            double xSize = xMax - xMin;
            double ySize = xSize / aspectRatio;
            _yMin = -ySize / 2;
            _yMax = ySize / 2;

            _data = new double[threadCount][];
            for (int i = 0; i < threadCount; i++)
            {
                _data[i] = new double[width * height];
            }

            _nx = 1 / xSize * width;
            _ny = 1 / ySize * height;
        }

        public double[][] Run()
        {
            var tasks = Enumerable
                .Range(0, threadCount)
                .Select(thread => Task.Run(() => Run(_data[thread], new Random(thread))))
                .ToArray();

            Console.WriteLine(tasks.Length);
            Task.WaitAll(tasks);
            return _data;
        }

        private void Run(double[] array, Random random)
        {
            long currentCount = 0;
            while (true)
            {
                var x = random.NextDouble() * 2 * (_xMax - _xMin) + _xMin;
                var y = random.NextDouble() * 2 * (_yMax - _yMin) + _yMin;

                double zr = 0.0, zi = 0.0, cr = x, ci = y;

                var escape = false;
                // have we escaped
                for (var i = 0; i < iterations; i++)
                {
                    var zzr = zr * zr - zi * zi;
                    var zzi = zr * zi + zi * zr;
                    zr = zzr + cr;
                    zi = zzi + ci;
                    escape = (zr * zr + zi * zi) > 4;
                    if (escape) {break;}
                }

                if (escape)
                {
                    zr = 0; zi = 0;
                    for (var i = 0; i < iterations; i++)
                    {
                        var zzr = zr * zr - zi * zi;
                        var zzi = zr * zi + zi * zr;
                        zr = zzr + cr;
                        zi = zzi + ci;

                        if ((zr * zr + zi * zi) > 14)
                            break;

                        IncreasePixel(array, zr, zi);
                        IncreasePixel(array, zr, -zi);
                    }
                }

                currentCount++;

                if (currentCount % 10000000 != 0) continue;

                Interlocked.Add(ref _total, currentCount);
                currentCount = 0;

                if (_total >= _samples)
                {
                    return;
                }
            }
        }

        private void IncreasePixel(double[] arr, double x, double y)
        {
            // don't increase if not in range
            if (x >= _xMax || x < _xMin) { return; }
            if (y >= _yMax || y < _yMin) { return; }
              // find the pixel
            var nx = (int)((x - _xMin) * _nx);
            var ny = (int)((y - _yMin) * _ny);
            var idx = nx + ny * width;
            arr[idx]++;
        }
    }
}
