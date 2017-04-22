Sorting Large Files in Constant Space
=============

The following examples shows how to use the system to sort large files in constant space

```csharp
public class SortingDis : Job
{
	public override void Main(string[] args)
	{
		var lineCount = 0;
		var filename = args[0];
		// File.OpenText() does not read the entire thing into memory
		using (var reader = File.OpenText(filename))
		{
			// Count the number of lines in the file
			while (reader.ReadLine() != null)
			{
				lineCount++;
			}
		}
		// Number of tasks to create is dynamic
		var splitSize = int.Parse(args[1]);
		for (var i = 0; i < splitSize; i++)
		{
			AddTask(new SortTask(filename, lineCount, i, splitSize));
		}
	}
	
	...
}
```

The SortingDis program extends the job class, is passed a large file with one item per line, and
the number of tasks to create.


```csharp
    public class SortTask : JobTask
    {
        private const int MaxMemory = 100000000;

        ...

        public SortTask(string filename,
            int lineCount, int section, int splitSize)
        {
			// init variables
            ...
			//
        }

        public override void Main(string[] args)
        {
            // split the file up into smaller pieces
            Split();
            // create a job result from the sorted pieces
            var jobResult = new SortResult { ResultPaths = Sort() };
            // add the result to our results
            AddResult(jobResult);
        }
	}
```

The SortTask class does the actual work of sorting some portion of the file

```csharp
    public class SortResult : JobResult
    {
        public List<string> ResultPaths { get; set; }
    }
```

The SortResult extends a JobResult which allows it to be aggregated and sent back. JobResult
uses Json serialization under the hood.


```csharp
// within SortingDis
public override void RunFinalTask()
{
	...
	// Merge the resulting tasks
	...
}
```

The SortingDis class overrides the RunFinalTask() method which runs on the same node
the Job was submitted on at the very end using anything within Results which is generated from all
the nodes completing tasks.


