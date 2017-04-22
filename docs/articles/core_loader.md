CoreLoader<T>
=============

For documentation see [CoreLoader<T>](https://acf5118.github.io/MS-Project/api/Defcore.Distributed.Assembly.CoreLoader-1.html)

The CoreLoader<T> class allows a user to load up and initialize a class from an assembly (dll)
and call methods and properties out of that class. 

Say you had a class Foo that looked like

```csharp
public class Foo
{
	// Some C# Property
	public int MyIntegerProperty { get; set; }
	
	// A method with no return type and no parameters
	public void MySimpleMethod() { Console.WriteLine("Simple enough"); }
	
	// A method with return type and multiple arguments of different types
	public string MyLessSimpleMethod(int arg1, string arg2, double arg3)
	{
		return arg2 + arg1 + arg3;
	}
}
```

Using CoreLoader<Foo> you could call methods and properties out of that class.

```csharp
public static void Main(string[] args)
{
	CoreLoader<Foo> foo = new CoreLoader<Foo>("/Path/To/Dll/Containing/Foo.dll");
	int x = (int)foo.GetProperty("MyIntegerProperty");
	foo.CallMethod("MySimpleMethod", new object[] {});
	var result = (string) foo.CallMethod(
		"MyLessSimpleMethod", new object[] {1, "hello", 3.4});
}
```

The CoreLoader<T> is how the defcore system calls methods out of a Job class. 