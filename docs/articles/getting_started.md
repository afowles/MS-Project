Basics
=============

At its core the simplest way to create
a distributed program is to create a class that
inherits from job, and then create a second program that
inherits from task.

```csharp
    public class Foo : Job
    {
        public override void Main(string[] args)
        {
            foreach (string s in args)
            {
                // do some setup
				...
				// create new tasks
                AddTask(new Bar(...));
            }
        }
        public class Bar : JobTask
        {
			public override void Main(string[] args)
			{
				// do some computational complex tasks
				// prime factorization or something
				...
			}
		}
```
inside the job program add some number of job tasks to be executed
by different nodes.