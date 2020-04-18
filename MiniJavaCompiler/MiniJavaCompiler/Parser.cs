﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

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

            var entry = symTable.Insert<MethodEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
            entry.ReturnType = VarType.voidType;

            Emit($"proc {entry.Lexeme}");

            Match(Symbol.maint);
            Match(Symbol.lparent);
            Match(Symbol.stringt);
            Match(Symbol.larrayt);
            Match(Symbol.rarrayt);
            Match(Symbol.idt);
            Match(Symbol.rparent);
            Match(Symbol.begint);
            SeqOfStatements(entry);
            Match(Symbol.endt);
            Match(Symbol.endt);

            Emit($"endp {entry.Lexeme}");
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

            var entry = symTable.Insert<ClassEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);

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
                        var intEntry = symTable.Insert<ConstEntry<int>>(varName, varToken, currentDepth);
                        intEntry.TypeOfConstant = constType;
                        intEntry.Value = int.Parse(analyzer.Lexeme);
                        intEntry.Offset = currentOffset;
                    }
                    else if (constType == VarType.floatType)
                    {
                        var floatEntry = symTable.Insert<ConstEntry<float>>(varName, varToken, currentDepth);
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
        }

        private static void IdentifierList(VarType type, int size, TableEntry parentEntry)
        {

            if (parentEntry is ClassEntry)
            {
                (parentEntry as ClassEntry).VariableNames.Add(analyzer.Lexeme);
                (parentEntry as ClassEntry).SizeOfLocals += size;
            }
            else if (parentEntry is MethodEntry)
            {
                (parentEntry as MethodEntry).SizeOfLocals += size;
            }
            var varEntry = symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
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

                var entry = symTable.Insert<MethodEntry>(analyzer.Lexeme, analyzer.Token, currentDepth);
                entry.ReturnType = type;
                parentEntry.MethodNames.Add(analyzer.Lexeme);

                Emit($"proc {entry.Lexeme}");

                Match(Symbol.idt);
                Match(Symbol.lparent);
                FormalList(entry);
                Match(Symbol.rparent);
                Match(Symbol.begint);
                VarDecl(entry);
                SeqOfStatements(entry);
                Match(Symbol.returnt);
                var retEntry = symTable.Lookup(analyzer.Lexeme);
                Expr(ref retEntry, entry);
                Match(Symbol.semit);
                Match(Symbol.endt);

                Emit($"endp {entry.Lexeme}");
                Emit($""); // space
                currentOffset = 0;

                MethodDecl(parentEntry);
            }
            currentOffset = 0;
        }

        private static void FormalList(MethodEntry entry)
        {
            if (analyzer.Token == Symbol.intt || analyzer.Token == Symbol.booleant || analyzer.Token == Symbol.floatt)
            {
                var (type, size) = Type();

                var varEntry = symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth + 1);
                varEntry.Offset = 0;
                varEntry.Size = size;
                varEntry.TypeOfVariable = type;
                entry.ParamList.Add((type, varEntry.Lexeme));
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

                var varEntry = symTable.Insert<VarEntry>(analyzer.Lexeme, analyzer.Token, currentDepth + 1);
                varEntry.Offset = offset;
                varEntry.Size = size;
                varEntry.TypeOfVariable = type;
                entry.ParamList.Add((type, varEntry.Lexeme));
                entry.SizeOfParameters += size;

                Match(Symbol.idt);
                FormalRest(entry, offset + size); 
            }
        }

        private static void SeqOfStatements(MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.idt)
            {
                Statement(parentEntry);
                Match(Symbol.semit);
                StatTail(parentEntry);
            }
        }

        private static void StatTail(MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.idt)
            {
                Statement(parentEntry);
                Match(Symbol.semit);
                StatTail(parentEntry);
            }
        }

        private static void Statement(MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.idt)
            {
                AssignStat(parentEntry);
            }
            else
            {
                IOStat(parentEntry);
            }
        }

        private static void AssignStat(MethodEntry parentEntry)
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
                    MethodCall(parentEntry);

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
                    Expr(ref entry, parentEntry);

                    if (entry != null && assignEntry != null)
                    {
                        Emit($"{GetBasePointerOffset(assignEntry, parentEntry)} = {GetBasePointerOffset(entry, parentEntry)}");
                    }
                }
            }
        }

        private static void IOStat(MethodEntry parentEntry)
        {
            // empty
        }

        private static void MethodCall(MethodEntry parentEntry)
        {
            ClassName();
            Match(Symbol.periodt);

            var entry = symTable.Lookup<MethodEntry>(analyzer.Lexeme);
            if (entry == null)
                throw new UndeclaredTokenException(analyzer.Lexeme);

            Match(Symbol.idt);
            Match(Symbol.lparent);
            var paramList = Params(parentEntry);
            Match(Symbol.rparent);

            for (int i = paramList.Count - 1; i >= 0; i--)
            {
                Emit($"push {paramList[i]}");
            }

            Emit($"call {entry}");
        }

        private static List<string> Params(MethodEntry parentEntry)
        {
            var paramList = new List<string>();
            if (analyzer.Token == Symbol.idt)
            {
                var entry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                if (entry == null)
                    throw new UndeclaredTokenException(analyzer.Lexeme);
                paramList.Add(GetBasePointerOffset(entry, parentEntry));

                Match(Symbol.idt);
                ParamsTail(paramList, parentEntry);
            }
            else if (analyzer.Token == Symbol.numt)
            {
                paramList.Add(analyzer.Lexeme);
                Match(Symbol.numt);
                ParamsTail(paramList, parentEntry);
            }

            return paramList;
        }

        private static void ParamsTail(List<string> paramList, MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.commat)
            {
                Match(Symbol.commat);
                if (analyzer.Token == Symbol.idt)
                {
                    var entry = symTable.Lookup<VarEntry>(analyzer.Lexeme);
                    if (entry == null)
                        throw new UndeclaredTokenException(analyzer.Lexeme);
                    paramList.Add(GetBasePointerOffset(entry, parentEntry));
                    Match(Symbol.idt);

                    ParamsTail(paramList, parentEntry);
                }
                else if (analyzer.Token == Symbol.numt)
                {
                    paramList.Add(analyzer.Lexeme);
                    Match(Symbol.numt);
                    ParamsTail(paramList, parentEntry);
                }
            }
        }

        private static void ClassName()
        {
            Match(Symbol.idt);
        }

        private static void Expr(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.idt ||
                analyzer.Token == Symbol.numt ||
                analyzer.Token == Symbol.lparent ||
                analyzer.Token == Symbol.nott ||
                analyzer.Token == Symbol.addopt ||
                analyzer.Token == Symbol.truet ||
                analyzer.Token == Symbol.falset)
            {
                Relation(ref entryRef, parentEntry);
            }

        }

        private static void Relation(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            SimpleExpr(ref entryRef, parentEntry);
        }

        private static void SimpleExpr(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            Term(ref entryRef, parentEntry);
            MoreTerm(ref entryRef, parentEntry);
        }

        private static void MoreTerm(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.addopt)
            {
                var entryTemp = NewTemp(entryRef.Token);
                var code = $"{GetBasePointerOffset(entryTemp, parentEntry)} = {GetBasePointerOffset(entryRef, parentEntry)} {analyzer.Lexeme} ";

                Match(Symbol.addopt);
                Term(ref entryRef, parentEntry);

                Emit(code + GetBasePointerOffset(entryRef, parentEntry));
                entryRef = entryTemp;

                MoreTerm(ref entryRef, parentEntry);
            }
        }

        private static void Term(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            Factor(ref entryRef, parentEntry);
            MoreFactor(ref entryRef, parentEntry);
        }

        private static void MoreFactor(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            if (analyzer.Token == Symbol.mulopt)
            {
                var entryTemp = NewTemp(entryRef.Token);
                var code = $"{GetBasePointerOffset(entryTemp, parentEntry)} = {GetBasePointerOffset(entryRef, parentEntry)} {analyzer.Lexeme} ";

                Match(Symbol.mulopt);
                Factor(ref entryRef, parentEntry);

                Emit(code + GetBasePointerOffset(entryRef, parentEntry));
                entryRef = entryTemp;

                MoreFactor(ref entryRef, parentEntry);
            }
        }

        private static void Factor(ref TableEntry entryRef, MethodEntry parentEntry)
        {
            switch (analyzer.Token)
            {
                case Symbol.idt:
                    var entry = symTable.Lookup(analyzer.Lexeme);
                    if (entry == null)
                        throw new UndeclaredTokenException(analyzer.Lexeme);
                    entryRef = entry;

                    Match(Symbol.idt);
                    break;
                case Symbol.numt:
                    entryRef = NewTemp(Symbol.numt);
                    Emit($"{GetBasePointerOffset(entryRef, parentEntry)} = {analyzer.Lexeme}");
                    Match(Symbol.numt);
                    break;
                case Symbol.lparent:
                    Match(Symbol.lparent);
                    Expr(ref entryRef, parentEntry);
                    Match(Symbol.rparent);
                    break;
                case Symbol.nott:
                    Match(Symbol.nott);
                    Factor(ref entryRef, parentEntry);
                    break;
                case Symbol.addopt:
                    SignOp();
                    var temp = NewTemp(Symbol.numt);
                    Emit($"{GetBasePointerOffset(temp, parentEntry)} = -1 * {GetBasePointerOffset(entryRef, parentEntry)}");
                    entryRef = temp;
                    Factor(ref entryRef, parentEntry);
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

        private static void SignOp()
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

        private static VarEntry NewTemp(Symbol tempToken)
        {
            var tempLexeme = $"_t{currentTempNum++}";
            var temp = symTable.Insert<VarEntry>(tempLexeme, tempToken, currentDepth);
            temp.Offset = currentOffset;
            temp.Size = 2;
            currentOffset += 2;
            return temp;
        }

        private static string GetBasePointerOffset(TableEntry entry, MethodEntry methodEntry)
        {
               
            if (entry is IStorable)
            {
                var offsetEntry = entry as IStorable;
                var isParam = methodEntry.ParamList.Any(param => entry.Lexeme == param.Item2);

                return isParam ? $"_bp+{offsetEntry.Offset + 4}" : $"_bp-{Math.Abs(methodEntry.SizeOfParameters - offsetEntry.Offset) + 2}";
            }
            throw new OtherParseException($"{entry} is not an offset variable.");
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
