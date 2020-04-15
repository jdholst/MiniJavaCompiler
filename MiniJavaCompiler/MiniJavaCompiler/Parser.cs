using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniJavaCompiler
{
    public static class Parser
    {
        private static LexicalAnalyzer analyzer;
        private static SymbolTable symTable;
        private static FileStream tacFile;

        private static int currentOffset = 0;
        private static int currentDepth = 0;
        private static int currentTempNum = 1;

        public static void Parse(string filePath)
        {
            try
            {
                symTable = new SymbolTable(211);
                analyzer = new LexicalAnalyzer(filePath);
                tacFile = File.Create(Path.GetFileNameWithoutExtension(filePath) + ".tac");
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
            catch (UndeclaredTokenException ex)
            {
                Console.WriteLine($"Error on line {analyzer.LineNo}: Undeclared identifier {ex.Lexeme} used");
            }
            catch (OtherParseException ex)
            {
                Console.WriteLine($"Error on line {analyzer.LineNo}: {ex.Message}");
            }
            finally
            {
                tacFile.Close();
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
                case Symbol.floatt:
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
                var retEntry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                Expr(ref retEntry);
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

                symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth + 1);
                var varEntry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                varEntry.Offset = 0;
                varEntry.Size = size;
                varEntry.TypeOfVariable = type;
                entry.ParamList.Add(type);
                entry.SizeOfParameters += size;
                Match(Symbol.idt);
                FormalRest(entry, size); 
            }
        }

        private static void FormalRest(MethodEntry entry, int offset)
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                var (type, size) = Type();

                symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth + 1);
                var varEntry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                varEntry.Offset = offset;
                varEntry.Size = size;
                varEntry.TypeOfVariable = type;
                entry.ParamList.Add(type);
                entry.SizeOfParameters += size;

                Match(Symbol.idt);
                FormalRest(entry, offset + size); 
            }
        }

        private static void SeqOfStatements()
        {
            if (analyzer.Token == Symbol.idt)
            {
                Statement();
                Match(Symbol.semit);
                StatTail();
            }
        }

        private static void StatTail()
        {
            if (analyzer.Token == Symbol.idt)
            {
                Statement();
                Match(Symbol.semit);
                StatTail();
            }
        }

        private static void Statement()
        {
            if (analyzer.Token == Symbol.idt)
            {
                AssignStat();
            }
            else
            {
                IOStat();
            }
        }

        private static void AssignStat()
        {
            if (analyzer.Token == Symbol.idt)
            {
                VarEntry assignEntry = null;

                var entry = symTable.Lookup(analyzer.Lexeme);
                if (entry == null)
                    throw new UndeclaredTokenException(analyzer.Lexeme);

                if (entry is VarEntry)
                {
                    Match(Symbol.idt);
                    Match(Symbol.assignopt);
                    assignEntry = entry as VarEntry;
                    entry = symTable.Lookup(analyzer.Lexeme);
                }

                if (entry is ClassEntry)
                {
                    MethodCall();

                    if (assignEntry != null)
                    {
                        Emit($"{assignEntry} = _AX");
                    }
                }
                else if (entry is MethodEntry)
                {
                    throw new OtherParseException("Invalid method call. Must specify class name before method call like so: ClassName.MethodName()");
                }
                else
                {
                    var varEntry = entry as VarEntry;
                    Expr(ref varEntry);

                    if (varEntry != null && assignEntry != null)
                    {
                        Emit($"{assignEntry} = {varEntry}");
                    }
                }
            }
        }

        private static void IOStat()
        {
            // empty
        }

        private static void MethodCall()
        {
            ClassName();
            Match(Symbol.periodt);

            var entry = symTable.Lookup<MethodEntry>(analyzer.Lexeme);
            if (entry == null)
                throw new UndeclaredTokenException(analyzer.Lexeme);

            Match(Symbol.idt);
            Match(Symbol.lparent);
            var paramList = Params();
            Match(Symbol.rparent);

            for (int i = paramList.Count - 1; i >= 0; i--)
            {
                Emit($"push {paramList[i]}");
            }

            Emit($"call {entry}");
        }

        private static List<string> Params()
        {
            var paramList = new List<string>();
            if (analyzer.Token == Symbol.idt)
            {
                var entry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                if (entry == null)
                    throw new UndeclaredTokenException(analyzer.Lexeme);
                paramList.Add(entry.Lexeme);

                Match(Symbol.idt);
                ParamsTail(paramList);
            }
            else if (analyzer.Token == Symbol.numt)
            {
                paramList.Add(analyzer.Lexeme);
                Match(Symbol.numt);
                ParamsTail(paramList);
            }

            return paramList;
        }

        private static void ParamsTail(List<string> paramList)
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                if (analyzer.Token == Symbol.idt)
                {
                    var entry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                    if (entry == null)
                        throw new UndeclaredTokenException(analyzer.Lexeme);
                    paramList.Add(entry.Lexeme);
                    Match(Symbol.idt);

                    ParamsTail(paramList);
                }
                else if (analyzer.Token == Symbol.numt)
                {
                    paramList.Add(analyzer.Lexeme);
                    Match(Symbol.numt);
                    ParamsTail(paramList);
                }
            }
        }

        private static void ClassName()
        {
            Match(Symbol.idt);
        }

        private static void Expr(ref VarEntry entryRef)
        {
            if (analyzer.Token == Symbol.idt ||
                analyzer.Token == Symbol.numt ||
                analyzer.Token == Symbol.lparent ||
                analyzer.Token == Symbol.nott ||
                analyzer.Token == Symbol.addopt ||
                analyzer.Token == Symbol.truet ||
                analyzer.Token == Symbol.falset)
            {
                Relation(ref entryRef);
            }

        }

        private static void Relation(ref VarEntry entryRef)
        {
            SimpleExpr(ref entryRef);
        }

        private static void SimpleExpr(ref VarEntry entryRef)
        {
            Term(ref entryRef);
            MoreTerm(ref entryRef);
        }

        private static void MoreTerm(ref VarEntry entryRef)
        {
            if (analyzer.Token == Symbol.addopt)
            {
                var entryTemp = NewTemp(entryRef);
                var code = $"{entryTemp} = {entryRef} {analyzer.Lexeme} ";

                Match(Symbol.addopt);
                Term(ref entryRef);

                Emit(code + entryRef);
                entryRef = entryTemp;

                MoreTerm(ref entryRef);
            }
        }

        private static void Term(ref VarEntry entryRef)
        {
            Factor(ref entryRef);
            MoreFactor(ref entryRef);
        }

        private static void MoreFactor(ref VarEntry entryRef)
        {
            if (analyzer.Token == Symbol.mulopt)
            {
                var entryTemp = NewTemp(entryRef);
                var code = $"{entryTemp} = {entryRef} {analyzer.Lexeme} ";

                Match(Symbol.mulopt);
                Factor(ref entryRef);

                Emit(code + entryRef);
                entryRef = entryTemp;

                MoreFactor(ref entryRef);
            }
        }

        private static void Factor(ref VarEntry entryRef)
        {
            switch (analyzer.Token)
            {
                case Symbol.idt:
                    var entry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                    if (entry == null)
                        throw new UndeclaredTokenException(analyzer.Lexeme);
                    entryRef = entry;

                    Match(Symbol.idt);
                    break;
                case Symbol.numt:
                    Match(Symbol.numt);
                    break;
                case Symbol.lparent:
                    Match(Symbol.lparent);
                    Expr(ref entryRef);
                    Match(Symbol.rparent);
                    break;
                case Symbol.nott:
                    Match(Symbol.nott);
                    Factor(ref entryRef);
                    break;
                case Symbol.addopt:
                    SignOp(ref entryRef);
                    Factor(ref entryRef);
                    break;
                case Symbol.truet:
                    Match(Symbol.truet);
                    break;
                case Symbol.falset:
                    Match(Symbol.falset);
                    break;
                default:
                    throw new UnexpectedTokenException(
                        analyzer.Token,
                        Symbol.idt,
                        Symbol.numt,
                        Symbol.lparent,
                        Symbol.nott,
                        Symbol.addopt,
                        Symbol.truet,
                        Symbol.falset);
            }
        }

        private static void SignOp(ref VarEntry entryRef)
        {
            var lexeme = analyzer.Lexeme;
            Match(Symbol.addopt);

            if (lexeme != "-")
            {
                throw new OtherParseException($"Invalid addopt used for signop. Must use \"-\" sign.");
            }
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

        private static void Emit(string code)
        {
            code += '\n';
            tacFile.Write(Encoding.ASCII.GetBytes(code));
        }

        private static VarEntry NewTemp(VarEntry other)
        {
            var tempLexeme = $"_t{currentTempNum++}";
            symTable.Insert<VarEntry>(tempLexeme, other.Token, other.Depth);
            return symTable.Lookup<VarEntry>(tempLexeme);
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

    public class UndeclaredTokenException: Exception
    {
        public string Lexeme { get; private set; }

        public UndeclaredTokenException(string lexeme): base()
        {
            Lexeme = lexeme;
        }
    }

    // Used for less frequent errors. Allows user to pass in custom message
    public class OtherParseException: Exception
    {
        public OtherParseException(string message) : base(message) { }
    }
}
