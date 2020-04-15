using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Parse(args[0]);
        }
    }
}
