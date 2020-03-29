using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var analyzer = new LexicalAnalyzer(args[0]);
            Parser.Parse(analyzer);
        }
    }
}
