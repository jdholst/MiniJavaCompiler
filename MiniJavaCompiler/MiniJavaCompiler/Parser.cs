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
        private static int currentDepth = 0;

        public static void Parse(LexicalAnalyzer lexicalAnalyzer)
        {
            try
            {
                symTable = new SymbolTable(211);
                analyzer = lexicalAnalyzer;
                analyzer.GetNextToken(); // prime the parser
                Prog();
            }
            catch (UnexpectedTokenException ex)
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
                    Console.WriteLine($"Error on line {analyzer.LineNo}: {message}");
                }
            }
            catch (DuplicateLexemeException ex)
            {
                Console.WriteLine($"Error on line {analyzer.LineNo}: Duplicate lexeme {ex.Lexeme} encountered");
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

            symTable.Insert<ClassEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);

            Match(Symbol.idt);
            Match(Symbol.begint);
            Match(Symbol.publict);
            Match(Symbol.statict);
            Match(Symbol.voidt);

            symTable.Insert<MethodEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
            var entry = symTable.Lookup<MethodEntry>(analyzer.Lexeme);
            entry.ReturnType = VarType.voidType;

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

            symTable.Insert<ClassEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
            var entry = symTable.Lookup<ClassEntry>(analyzer.Lexeme);

            Match(Symbol.idt);

            if (analyzer.Token == Symbol.extendst)
            {
                Match(Symbol.extendst);
                Match(Symbol.idt);
            }

            Match(Symbol.begint);
            VarDecl(entry);
            MethodDecl(entry);
            Match(Symbol.endt);
        }

        private static void VarDecl(TableEntry parentEntry)
        {
            switch (analyzer.Token)
            {
                case Symbol.finalt:
                    Match(Symbol.finalt);
                    var (constType, constSize) = Type();
                    var varName = analyzer.Lexeme;
                    var varToken = analyzer.Token;

                    if (parentEntry is ClassEntry)
                    {
                        (parentEntry as ClassEntry).VariableNames.Add(varName);
                        (parentEntry as ClassEntry).SizeOfLocals += constSize;
                    }
                    else if (parentEntry is MethodEntry)
                    {
                        (parentEntry as MethodEntry).SizeOfLocals += constSize;
                    }

                    Match(Symbol.idt);
                    Match(Symbol.assignopt);

                    if (constType == VarType.intType)
                    {
                        symTable.Insert<ConstEntry<int>>(varName, varToken, currentDepth);
                        var intEntry = symTable.Lookup<ConstEntry<int>>(varName);
                        intEntry.TypeOfConstant = constType;
                        intEntry.Value = int.Parse(analyzer.Lexeme);
                        intEntry.Offset = currentOffset;
                    }
                    else if (constType == VarType.floatType)
                    {
                        symTable.Insert<ConstEntry<float>>(varName, varToken, currentDepth);
                        var floatEntry = symTable.Lookup<ConstEntry<float>>(varName);
                        floatEntry.TypeOfConstant = constType;
                        floatEntry.Value = float.Parse(analyzer.Lexeme);
                        floatEntry.Offset = currentOffset;
                    }
                    currentOffset += constSize;

                    Match(Symbol.numt);
                    Match(Symbol.semit);
                    VarDecl(parentEntry);
                    break;
                case Symbol.intt:
                case Symbol.booleant:
                    var (varType, varSize) = Type();

                    IdentifierList(varType, varSize, parentEntry);
                    Match(Symbol.semit);
                    VarDecl(parentEntry);
                    break;
            }

            currentOffset = 0;
        }

        private static void IdentifierList(VarType type, int size, TableEntry parentEntry)
        {
            symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);

            if (parentEntry is ClassEntry)
            {
                (parentEntry as ClassEntry).VariableNames.Add(analyzer.Lexeme);
                (parentEntry as ClassEntry).SizeOfLocals += size;
            }
            else if (parentEntry is MethodEntry)
            {
                (parentEntry as MethodEntry).SizeOfLocals += size;
            }

            var varEntry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
            varEntry.TypeOfVariable = type;
            varEntry.Size = size;
            varEntry.Offset = currentOffset;
            currentOffset += size;

            Match(Symbol.idt);
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                IdentifierList(type, size, parentEntry);
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
                case Symbol.floatt:
                    Match(Symbol.floatt);
                    type = VarType.floatType;
                    size = 4;
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
                    throw new UnexpectedTokenException(analyzer.Token, Symbol.intt, Symbol.booleant, Symbol.voidt);
            }

            return (type, size);
        }

        private static void MethodDecl(ClassEntry parentEntry)
        {
            if (analyzer.Token == Symbol.publict)
            {
                Match(Symbol.publict);
                var (type, _) = Type();

                symTable.Insert<MethodEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
                var entry = symTable.Lookup<MethodEntry>(analyzer.Lexeme);
                entry.ReturnType = type;
                parentEntry.MethodNames.Add(analyzer.Lexeme);

                Match(Symbol.idt);
                Match(Symbol.lparent);
                FormalList(entry);
                Match(Symbol.rparent);
                Match(Symbol.begint);
                VarDecl(entry);
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
            if (analyzer.Token == Symbol.intt || analyzer.Token == Symbol.booleant || analyzer.Token == Symbol.floatt)
            {
                var (type, size) = Type();
                entry.ParamList.Add(type);
                entry.SizeOfParameters += size;
                Match(Symbol.idt);
                FormalRest(entry); 
            }
        }

        private static void FormalRest(MethodEntry entry)
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                var (type, size) = Type();
                entry.ParamList.Add(type);
                entry.SizeOfParameters += size;
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
                if (desired == Symbol.begint)
                {
                    currentDepth++;
                }
                else if (desired == Symbol.endt || desired == Symbol.eoft)
                {
                    symTable.WriteTable(currentDepth);
                    symTable.DeleteDepth(currentDepth);
                    if (currentDepth > 0)
                        currentDepth--;
                }

                analyzer.GetNextToken();
            }
            else
            {
                throw new UnexpectedTokenException(analyzer.Token, desired);
            }
        }
    }

    public class UnexpectedTokenException: Exception
    {
        public Symbol[] Expected { get; private set; }
        public Symbol Actual { get; private set; }
        public UnexpectedTokenException(Symbol actual, params Symbol[] expected)
        {
            Expected = expected;
            Actual = actual;
        }
    }
}
