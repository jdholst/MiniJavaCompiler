using System;
using System.Collections.Generic;
using System.Text;

namespace MiniJavaCompiler
{
    public static class Parser
    {
        private static LexicalAnalyzer analyzer;
        private static SymbolTable symTable;

        private static int currentOffset = 0;

        public static SymbolTable Parse(LexicalAnalyzer lexicalAnalyzer)
        {
            try
            {
                symTable = new SymbolTable(32);
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
                    Console.WriteLine($"Error on line {ex.LineNo}: {message}");
                }
            }

            return symTable;
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

            symTable.Insert<ClassEntry>(analyzer.Lexeme, analyzer.Token, 0);
            var entry = symTable.Lookup<ClassEntry>(analyzer.Lexeme); //.Value;
            entry.TypeOfEntry = EntryType.classEntry;

            Match(Symbol.idt);

            if (analyzer.Token == Symbol.extendst)
            {
                Match(Symbol.extendst);
                Match(Symbol.idt);
            }

            Match(Symbol.begint);
            VarDecl(entry);

            entry.SizeOfLocals = currentOffset;
            currentOffset = 0;

            MethodDecl(entry);
            Match(Symbol.endt);
        }

        private static void VarDecl(ClassEntry parentEntry = null)
        {
            switch (analyzer.Token)
            {
                case Symbol.finalt:
                    Match(Symbol.finalt);
                    var (constType, constSize) = Type();
                    var varName = analyzer.Lexeme;
                    var varToken = analyzer.Token;
                    parentEntry?.VariableNames.Add(varName);

                    Match(Symbol.idt);
                    Match(Symbol.assignopt);

                    if (constType == VarType.intType)
                    {
                        symTable.Insert<ConstEntry<int>>(varName, varToken, 0);
                        var intEntry = symTable.Lookup<ConstEntry<int>>(analyzer.Lexeme); //.Value;
                        intEntry.TypeOfEntry = EntryType.constEntry;
                        intEntry.TypeOfConstant = constType;
                        intEntry.Value = int.Parse(analyzer.Lexeme);
                        intEntry.Offset = currentOffset;
                    }
                    else if (constType == VarType.floatType)
                    {
                        symTable.Insert<ConstEntry<int>>(varName, varToken, 0);
                        var floatEntry = symTable.Lookup<ConstEntry<float>>(analyzer.Lexeme); //.Value;
                        floatEntry.TypeOfEntry = EntryType.constEntry;
                        floatEntry.TypeOfConstant = constType;
                        floatEntry.Value = float.Parse(analyzer.Lexeme);
                        floatEntry.Offset = currentOffset;
                    }
                    currentOffset += constSize;

                    Match(Symbol.numt);
                    Match(Symbol.semit);
                    VarDecl();
                    break;
                case Symbol.intt:
                case Symbol.booleant:
                    var (varType, varSize) = Type();

                    IdentifierList(varType, varSize);
                    Match(Symbol.semit);
                    VarDecl();
                    break;
            }
        }

        private static void IdentifierList(VarType type, int size)
        {
            symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, 0);
            var varEntry = symTable.Lookup<VarEntry>(analyzer.Lexeme); //.Value;
            varEntry.TypeOfVariable = type;
            varEntry.Size = size;
            varEntry.Offset = currentOffset;
            currentOffset += size;

            Match(Symbol.idt);
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                IdentifierList(type, size);
            }
        }

        private static (VarType, int) Type()
        {
            VarType type;
            int size = 0;

            switch (analyzer.Token)
            {
                case Symbol.intt:
                    Match(Symbol.intt);
                    type = VarType.intType;
                    size = 2;
                    break;
                case Symbol.booleant:
                    Match(Symbol.booleant);
                    type = VarType.booleanType;
                    size = 1;
                    break;
                case Symbol.voidt:
                    Match(Symbol.voidt);
                    type = VarType.voidType;
                    break;
                default:
                    throw new ParseException(analyzer.Token, analyzer.LineNo, Symbol.intt, Symbol.booleant, Symbol.voidt);
            }

            return (type, size);
        }

        private static void MethodDecl(ClassEntry parentEntry)
        {
            if (analyzer.Token == Symbol.publict)
            {
                Match(Symbol.publict);
                var (type, _) = Type();

                symTable.Insert<MethodEntry>(analyzer.Lexeme, analyzer.Token, 0);
                var entry = symTable.Lookup<MethodEntry>(analyzer.Lexeme);// .Value;
                entry.TypeOfEntry = EntryType.methodEntry;
                entry.ReturnType = type;
                parentEntry.MethodNames.Add(analyzer.Lexeme);

                Match(Symbol.idt);
                Match(Symbol.lparent);
                FormalList(entry);
                Match(Symbol.rparent);
                Match(Symbol.begint);
                VarDecl();

                entry.SizeOfLocals = currentOffset;
                currentOffset = 0;

                SeqOfStatements();
                Match(Symbol.returnt);
                Expr();
                Match(Symbol.semit);
                Match(Symbol.endt);
                MethodDecl(parentEntry);
            }
        }

        private static void FormalList(MethodEntry entry)
        {
            if (analyzer.Token == Symbol.intt || analyzer.Token == Symbol.booleant)
            {
                var (type, _) = Type();
                entry.ParamList.Add(type);
                Match(Symbol.idt);
                FormalRest(entry); 
            }
        }

        private static void FormalRest(MethodEntry entry)
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                var (type, _) = Type();
                entry.ParamList.Add(type);
                Match(Symbol.idt);
                FormalRest(entry); 
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

        private static void Match(Symbol desired)
        {
            if (analyzer.Token == desired)
            {
                analyzer.GetNextToken();
            }
            else
            {
                throw new ParseException(analyzer.Token, analyzer.LineNo, desired);
            }
        }
    }

    public class ParseException: Exception
    {
        public Symbol[] Expected { get; private set; }
        public Symbol Actual { get; private set; }
        public int LineNo { get; private set; }
        public ParseException(Symbol actual, int lineNo, params Symbol[] expected)
        {
            Expected = expected;
            Actual = actual;
            LineNo = lineNo;
        }
    }
}
