using System;
using System.Collections.Generic;
using System.Text;

namespace MiniJavaCompiler
{
    public static class Parser
    {
        private static LexicalAnalyzer analyzer;

        public static void Parse(LexicalAnalyzer lexicalAnalyzer)
        {
            try
            {
                analyzer = lexicalAnalyzer;
                analyzer.GetNextToken(); // prime the parser
                Prog();
            }
            catch (ParseException ex)
            {
                if (ex.Expected[0] == Symbol.eoft)
                {
                    Console.WriteLine("Error: Unused tokens");
                }
                else
                {
                    string message = $"Expected {ex.Expected[0]} ";

                    for (int i = 1; i < ex.Expected.Length; i++)
                    {
                        message += $"or {ex.Expected[i]} ";
                    }

                    message += $"but read {ex.Actual}";
                    Console.WriteLine($"Error: {message}");
                }
            }
        }

        private static void Prog()
        {
            MoreClasses();
            MainClass();
            Match(Symbol.eoft);
        }

        private static void MainClass()
        {
            Match(Symbol.finalt);
            Match(Symbol.classt);
            Match(Symbol.idt);
            Match(Symbol.begint);
            Match(Symbol.publict);
            Match(Symbol.statict);
            Match(Symbol.voidt);
            Match(Symbol.maint);
            Match(Symbol.lparent);
            Match(Symbol.stringt);
            Match(Symbol.larrayt);
            Match(Symbol.rarrayt);
            Match(Symbol.idt);
            Match(Symbol.rparent);
            Match(Symbol.begint);
            SeqOfStatements();
            Match(Symbol.endt);
            Match(Symbol.endt);
        }
        
        private static void MoreClasses()
        {
            if (analyzer.Token == Symbol.classt)
            {
                ClassDecl();
                MoreClasses();
            }
        }

        private static void ClassDecl()
        {
            Match(Symbol.classt);
            Match(Symbol.idt);

            if (analyzer.Token == Symbol.extendst)
            {
                Match(Symbol.extendst);
                Match(Symbol.idt);
            }

            Match(Symbol.begint);
            VarDecl();
            MethodDecl();
            Match(Symbol.endt);
        }

        private static void VarDecl()
        {
            if (analyzer.Token == Symbol.finalt)
            {
                Match(Symbol.finalt);
                Type();
                Match(Symbol.idt);
                Match(Symbol.assignopt);
                Match(Symbol.numt);
                Match(Symbol.semit);
                VarDecl();
            }
            else if (analyzer.Token == Symbol.intt || analyzer.Token == Symbol.booleant)
            {
                Type();
                IdentifierList();
                Match(Symbol.semit);
                VarDecl();
            }
        }

        private static void IdentifierList()
        {
            Match(Symbol.idt);
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                IdentifierList();
            }
        }

        private static void Type()
        {
            Match(Symbol.intt, Symbol.booleant, Symbol.voidt);
        }

        private static void MethodDecl()
        {
            if (analyzer.Token == Symbol.publict)
            {
                Match(Symbol.publict);
                Type();
                Match(Symbol.idt);
                Match(Symbol.lparent);
                FormalList();
                Match(Symbol.rparent);
                Match(Symbol.begint);
                VarDecl();
                SeqOfStatements();
                Match(Symbol.returnt);
                Expr();
                Match(Symbol.semit);
                Match(Symbol.endt); 
            }
        }

        private static void FormalList()
        {
            if (analyzer.Token == Symbol.intt || analyzer.Token == Symbol.booleant)
            {
                Type();
                Match(Symbol.idt);
                FormalRest(); 
            }
        }

        private static void FormalRest()
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                Type();
                Match(Symbol.idt);
                FormalRest(); 
            }
        }

        private static void SeqOfStatements()
        {
            // empty
        }

        private static void Expr()
        {
            // empty
        }

        private static void Match(params Symbol[] desired)
        {
            bool matched = false;
            foreach (var sym in desired)
            {
                if (analyzer.Token == sym)
                {
                    analyzer.GetNextToken();
                    matched = true;
                    break;
                }
            }

            if (!matched) throw new ParseException(desired, analyzer.Token);
        }
    }

    public class ParseException: Exception
    {
        public Symbol[] Expected { get; private set; }
        public Symbol Actual { get; private set; }
        public ParseException(Symbol[] expected, Symbol actual)
        {
            Expected = expected;
            Actual = actual;
        }
    }
}
