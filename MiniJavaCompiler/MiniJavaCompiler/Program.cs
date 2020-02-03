using System;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexical = new LexicalAnalyzer();

            lexical.GetNextToken();
            Console.WriteLine(lexical.Token);

            lexical.GetNextToken();
            Console.WriteLine(lexical.Token);

            lexical.GetNextToken();
            Console.WriteLine(lexical.Token);

            lexical.GetNextToken();
            Console.WriteLine(lexical.Token);

            lexical.GetNextToken();
            Console.WriteLine(lexical.Token);
        }
    }
}
