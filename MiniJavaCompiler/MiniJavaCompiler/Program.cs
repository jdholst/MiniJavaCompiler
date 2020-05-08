using System;
using System.IO;

namespace MiniJavaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var (outputTACPath, symTable) = Parser.Parse(args[0]);
            Assembler.Assemble8086(outputTACPath, symTable);

            //var mockSymTable = new SymbolTable(211);

            //mockSymTable.Insert<MethodEntry>("main", Symbol.maint, 1);
            //mockSymTable.Insert<MethodEntry>("firstclass", Symbol.idt, 1);
            //var secondClass = mockSymTable.Insert<MethodEntry>("secondclass", Symbol.idt, 1);
            //secondClass.SizeOfLocals = 12;

            //var s0 = mockSymTable.Insert<LiteralEntry>("S0", Symbol.quotet, 2);
            //s0.Literal = "Enter a number";

            //var s1 = mockSymTable.Insert<LiteralEntry>("S1", Symbol.quotet, 2);
            //s1.Literal = "The answer is ";

            //Assembler.Assemble8086("example.tac", mockSymTable);
        }
    }
}
