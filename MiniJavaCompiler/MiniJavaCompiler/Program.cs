using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Error: Missing program file-name arugment");

            var lexical = new LexicalAnalyzer(args[0]);

            Parser.Parse(lexical);
        }
    }
}
