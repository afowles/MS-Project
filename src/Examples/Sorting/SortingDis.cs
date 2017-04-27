using System;
using System.Collections.Generic;
using System.IO;
using Defcore.Distributed.Jobs;

namespace Examples.Sorting
{
    public class SortingDis : Job
    {

        public override void Main(string[] args)
        {
            Console.WriteLine("hello there friends");
            // Count the number of lines in the file
            var lineCount = 0;
            var filename = args[0];
            // File.OpenText() does not read the entire thing into memory
            using (var reader = File.OpenText(filename))
            {
                while (reader.ReadLine() != null)
                {
                    lineCount++;
                }
            }
            // Number of tasks to create is dynamic
            var splitSize = int.Parse(args[1]);
            // split them into some number of groups
            for (var i = 0; i < splitSize; i++)
            {
                AddTask(new SortTask(filename, lineCount, i, splitSize));
            }
        }

        public override void RunFinalTask()
        {
            var paths = new List<string>();
            var dir = "";
            foreach(JobResult job in Results)
            {

                var t = job as SortResult;
                if (t == null) { Console.WriteLine("bad file");
                    return;
                }
                dir = t.Directory;
                paths.AddRange(t.ResultPaths);
            }

            int chunks = paths.Count; // Number of chunks
            int recordsize = 100; // estimated record size
            int records = 10000000; // estimated total # records
            int maxusage = 500000000; // max memory usage
            int buffersize = maxusage / chunks; // bytes of each queue
            double recordoverhead = 7.5; // The overhead of using Queue<>
            int bufferlen = (int)(buffersize / recordsize /
              recordoverhead); // number of records in each queue

            // Open the files
            StreamReader[] readers = new StreamReader[chunks];
            for (int i = 0; i < chunks; i++)
                readers[i] = new StreamReader(File.Open(paths[i], FileMode.Open));
            

            // Make the queues
            Queue<string>[] queues = new Queue<string>[chunks];
            for (int i = 0; i < chunks; i++)
                queues[i] = new Queue<string>(bufferlen);

            // Load the queues
            for (int i = 0; i < chunks; i++)
                LoadQueue(queues[i], readers[i], bufferlen);

            // Merge!
            StreamWriter sw = new StreamWriter(File.Create(dir+"/Sorted.txt"));
            bool done = false;
            int lowest_index, j, progress = 0;
            string lowest_value;
            while (!done)
            {
                // Report the progress
                if (++progress % 5000 == 0)
                    Console.Write("{0:f2}%   \r",
                      100.0 * progress / records);

                // Find the chunk with the lowest value
                lowest_index = -1;
                lowest_value = "";
                for (j = 0; j < chunks; j++)
                {
                    if (queues[j] != null)
                    {
                        if (lowest_index < 0 ||
                          String.CompareOrdinal(
                            queues[j].Peek(), lowest_value) < 0)
                        {
                            lowest_index = j;
                            lowest_value = queues[j].Peek();
                        }
                    }
                }

                // Was nothing found in any queue? We must be done then.
                if (lowest_index == -1) { done = true; break; }

                // Output it
                sw.WriteLine(lowest_value);

                // Remove from queue
                queues[lowest_index].Dequeue();
                // Have we emptied the queue? Top it up
                if (queues[lowest_index].Count == 0)
                {
                    LoadQueue(queues[lowest_index],
                      readers[lowest_index], bufferlen);
                    // Was there nothing left to read?
                    if (queues[lowest_index].Count == 0)
                    {
                        queues[lowest_index] = null;
                    }
                }
            }
            sw.Dispose();

            // Close and delete the files
            for (int i = 0; i < chunks; i++)
            {
                readers[i].Dispose();
                File.Delete(paths[i]);
            }
        }

        private static void LoadQueue(Queue<string> queue, 
            StreamReader file, int records)
        {
            for (int i = 0; i < records; i++)
            {
                if (file.Peek() < 0)
                {
                    Console.WriteLine("hello");
                    break;
                    
                }
                queue.Enqueue(file.ReadLine());
            }
        }
    }

    public class SortTask : JobTask
    {
        private const int MaxMemory = 100000000;

        private readonly string _filename;
        private readonly string _dir;
        private readonly int _lineCount;
        private readonly int _section;
        private readonly int _splitSize;
        
        private readonly List<string> _resultPaths;

        public SortTask(string filename,
            int lineCount, int section, int splitSize)
        {
            _filename = filename;
            _dir = Path.GetDirectoryName(filename);
            _lineCount = lineCount;
            _section = section;
            _splitSize = splitSize;
            _resultPaths = new List<string>();
        }

        public override void Main(string[] args)
        {
            // split the file up into smaller pieces
            Split();
            // create a job result from the sorted pieces
            var jobResult = new SortResult { ResultPaths = Sort(), Directory = _dir };
            // add the result to our results
            AddResult(jobResult);
        }


        public void Split()
        {
            var lineCount = 0;
            // calculate the number of lines we are sorting
            var numLines = _lineCount / _splitSize;
            var sectionNum = 1;
            var path = string.Format("{0}/SplitSection{1}{2:d5}.txt", _dir, _section, sectionNum);
            // add the first path to the list
            _resultPaths.Add(path);

            var writer = new StreamWriter(File.Create(path));
            using (var reader = File.OpenText(_filename))
            {
                // get to where we want to be in the file
                do
                {
                    if (lineCount == numLines)
                    {
                        break;
                    }
                    lineCount++;
                }
                while (reader.ReadLine() != null);

                var linesToRead = 0;
                // read for a fixed section of until the end of the file
                while (reader.ReadLine() != null && linesToRead < numLines)
                {
                    // write a line
                    writer.WriteLine(reader.ReadLine());

                    if (writer.BaseStream.Length > MaxMemory && reader.Peek() >= 0)
                    {
                        writer.Dispose();
                        sectionNum++;
                        // add additional paths if nessesary
                        path = string.Format("{0}/SplitSection{1}{2:d5}.txt", _dir, _section, sectionNum);
                        writer = new StreamWriter(File.Create(path));
                        _resultPaths.Add(path);
                    }
                    linesToRead++;
                }
                writer.Dispose();
            }
            
        }

        /// <summary>
        /// Sort the resulting split files into chunks
        /// </summary>
        /// <returns></returns>
        public List<string> Sort()
        {
            var result = new List<string>();
            foreach (var file in _resultPaths)
            {
                // read into memory and sort, each file is < MaxMemory
                var toSort = File.ReadAllLines(file);
                // C# default sort
                Array.Sort(toSort);
                // save off in a different location
                var savedFile = file.Replace("Split", "Sorted");
                File.WriteAllLines(savedFile, toSort);
                result.Add(savedFile);
                // cleanup the unsorted file
                File.Delete(file);
            }
            return result;
        }
    }

    public class SortResult : JobResult
    {
        public List<string> ResultPaths { get; set; }
        public string Directory { get; set; }
    }
}
