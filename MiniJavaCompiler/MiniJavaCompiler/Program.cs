using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var analyzer = new LexicalAnalyzer(args[0]);
            var table = Parser.Parse(analyzer);
            var entry = table.Lookup<MethodEntry>("sum");
            Console.WriteLine(entry.TypeOfEntry);

            Console.WriteLine("Variable Names: ");
            entry.ParamList.ForEach(name => Console.WriteLine(name));
        }
    }
}
