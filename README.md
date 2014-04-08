# closure-externs-dotnet


Generate Javascript classes for your .NET types. As a bonus you get [Google closure annotations](https://developers.google.com/closure/compiler/docs/js-for-compiler#types). The type annotations are understood by WebStorm (and other editors) and improve your development experience. Also, if you use Google Closure to compile or verify your code, it will take these types into account.

ClosureExterns.NET makes it easier to keep your frontend models in sync with your backend. It can generate Javascript classes (constructor based) with type-annotated prototypical properties that Google Closure understands. The output is also understood by editors such as WebStorm. The output is customizable - you can change several aspects of the generated code. For example you can change the constructor function definition, to support inheritance from some other Javascript function. For more details see [ClosureExternOptions](https://github.com/bdb-opensource/closure-externs-dotnet/blob/master/ClosureExterns/ClosureExternsOptions.cs).
 

## Getting Started

First, install it. Using **nuget**, install the package [ClosureExterns](http://www.nuget.org/packages/ClosureExterns.NET/). 

Then, expose a method that generates your externs. For example, a console application:


    public static class Program
    {
        public static void Main()
        {
            var types = ClosureExternsGenerator.GetTypesInNamespace(typeof(MyNamespace.MyType));
            var output = ClosureExternsGenerator.Generate(types);
            Console.Write(output);
        }
    }


You can also customize the generation using a `ClosureExternsOptions` object. 

## Example input/output

### Input


    class B
    {
        public int[] IntArray { get; set; }
    }




### Output
    
    var Types = {};
    
    // ClosureExterns.Tests.ClosureExternsGeneratorTest+B
    /** @constructor
    */
    Types.B = function() {};
    /** @type {Array.<number>} */
    Types.B.prototype.intArray = null;
    
    
For a full example see [the tests](https://github.com/bdb-opensource/closure-externs-dotnet/blob/master/ClosureExterns.Tests/ClosureExternsGeneratorTest.cs).


