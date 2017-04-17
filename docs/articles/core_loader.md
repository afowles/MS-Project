Core Loader
=============

For documentation see [Core Loader](https://acf5118.github.io/MS-Project/api/Defcore.Distributed.Assembly.CoreLoader.html)

The CoreLoader<T> class allows you to load up and initialize a class from an assembly (dll)
and call methods and properties out of that class. 

Say you had a class Foo that looked like

```csharp
public class Foo
{
	public int MyIntegerProperty { get; set; }

	public void MySimpleMethod() { Console.WriteLine("Simple enough"); }

	public string MyLessSimpleMethod(int arg1, string arg2, double arg3)
	{
		return arg2 + arg1 + arg3;
	}
}
```

Using Core Loader you could call methods and properties out of that class.

```csharp
public static void Main(string[] args)
{
	CoreLoader<Foo> foo = new CoreLoader<Foo>("PathToDllContainingFoo");
	int x = (int)foo.GetProperty("MyIntegerProperty");
	foo.CallMethod("MySimpleMethod", new object[] {});
	var result = (string) foo.CallMethod(
		"MyLessSimpleMethod", new object[] {1, "hello", 3.4});
}
```

The Core Loader is how the defcore system calls methods out of a Job class. 