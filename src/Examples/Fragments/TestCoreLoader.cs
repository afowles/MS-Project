
using System;
using Defcore.Distributed.Assembly;

namespace Examples.CodeFragments
{

    internal class TestCoreLoader
    {
        public static void Main2(string[] args)
        {
            CoreLoader<Foo> foo = new CoreLoader<Foo>("PathToDllContainingFoo");
            int x = (int)foo.GetProperty("MyIntegerProperty");
            foo.CallMethod("MySimpleMethod", new object[] {});
            var result = (string) foo.CallMethod(
                "MyLessSimpleMethod", new object[] {1, "hello", 3.4});
        }
    }

    public class Foo
    {
        public int MyIntegerProperty { get; set; }

        public void MySimpleMethod() { Console.WriteLine("Simple enough"); }

        public string MyLessSimpleMethod(int arg1, string arg2, double arg3)
        {
            return arg2 + arg1 + arg3;
        }
    }

}