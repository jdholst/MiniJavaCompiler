using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexical = new LexicalAnalyzer(args[0]);

            lexical.GetAllTokensAndDisplay();
        }
    }
}
