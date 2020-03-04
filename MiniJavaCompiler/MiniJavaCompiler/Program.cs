using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var table = new SymbolTable(211);

            table.Insert("int", Symbol.intt, 0);
            table.Insert("boolean", Symbol.booleant, 1);
            Console.WriteLine(table.Lookup("boolean"));
        }
    }
}
