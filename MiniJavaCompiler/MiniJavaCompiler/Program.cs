using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileInput = new string[] { };
            if (args.Length <= 2)
            {
                fileInput = File.ReadAllLines(args[0]);
            }

            var lexical = new LexicalAnalyzer(fileInput);

            lexical.GetAllTokensAndDisplay();
        }
    }
}
