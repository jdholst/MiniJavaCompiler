using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MiniJavaCompiler
{

    public static class Assembler
    {
        // read objects
        private static StreamReader ilFileReader;
        private static SymbolTable symTable;

        // write objects
        private static FileStream assemblyFile;

        public static void Assemble8086(string intermediateFilePath, SymbolTable symbols)
        {
            ilFileReader = File.OpenText(intermediateFilePath);
            symTable = symbols;
            symbols.UseSavedTable = true;

            var asmFilePath = Path.GetFileNameWithoutExtension(intermediateFilePath) + ".asm";

            using (assemblyFile = File.Create(asmFilePath))
            {
                AddHeader();
                AddStartProc();
                Emit(""); // empty line
                AddMethods();
                Emit("END start");
            }
        }

        private static void AddHeader()
        {
            Emit(".model small");
            Emit(".stack 100h");
            Emit(".data");

            // declare string literals
            int i = 0;
            while (true)
            {
                var literal = symTable.Lookup<LiteralEntry>($"S{i++}");

                if (literal != null)
                {
                    Emit($"{literal} DB \"{literal.Literal}\", \"$\"");
                }
                else
                {
                    break;
                }
            }

            Emit(".code");
            Emit("include io.asm");
        }

        private static void AddStartProc()
        {
            Emit("start PROC");
            EmitInstruction("mov", "ax", "@data");
            EmitInstruction("mov", "ds", "ax");
            EmitInstruction("call", "main");
            EmitInstruction("mov", "ah", "04ch");
            EmitInstruction("int", "21h");
            Emit("start ENDP");
        }

        private static void AddMethods()
        {
            while (!ilFileReader.EndOfStream)
            {
                var procLine = ReadLine();

                var methodEntry = symTable.Lookup<MethodEntry>(procLine[1]);

                Emit($"{methodEntry} PROC");

                if (methodEntry.Token == Symbol.maint)
                {
                    AddMethodCode(methodEntry);
                    EmitInstruction("ret");
                }
                else
                {
                    EmitInstruction("push", "bp");
                    EmitInstruction("mov", "bp", "sp");
                    EmitInstruction("sub", "sp", methodEntry.SizeOfLocals.ToString());

                    AddMethodCode(methodEntry);

                    EmitInstruction("add", "sp", methodEntry.SizeOfLocals.ToString());
                    EmitInstruction("pop", "bp");
                    EmitInstruction("ret", methodEntry.SizeOfParameters.ToString());
                }
                
                Emit($"{methodEntry} ENDP");
                Emit(""); // empty line
            }
        }

        private static void AddMethodCode(MethodEntry entry)
        {
            var code = ReadLine();
            while (code.Length > 0 && code[0] != "endp")
            {
                if (code[0] == "wrs")
                {
                    WriteStringStat(code[1]);
                }
                else if (code[0] == "wri")
                {
                    WriteIntegerStat(code[1]);
                }
                else if (code[0] == "wrln")
                {
                    WriteLineStat();
                }
                else if (code[0] == "rdi")
                {
                    ReadIntegerStat(code[1]);
                }
                else if (code[0] == "call" || code[0] == "push")
                {
                    EmitInstruction(code[0], code[1]);
                }
                else if (code.Length > 2) // checking at index 1 here so ensure length is correct
                {
                    if (code[1] == "=")
                    {
                        if (code.Length == 3)
                        {
                            if (code[0] == "_ax")
                            {
                                ReturnStat(code[2]);
                            }
                            else if (code[2] == "_ax")
                            {
                                AssignFromReturnStat(code[0]);
                            }
                            else
                            {
                                CopyStat(code[0], code[2]);
                            }
                        }
                        else if (code.Length == 5)
                        {
                            ArithmeticStat(code[0], code[2], code[3], code[4]);
                        }
                    }
                }

                code = ReadLine();
            }
        }

        private static void WriteStringStat(string globalStringName)
        {
            EmitInstruction("mov", "dx", $"offset {globalStringName}");
            EmitInstruction("call", "writestr");
        }

        private static void WriteIntegerStat(string op)
        {
            EmitInstruction("mov", "dx", op);
            EmitInstruction("call", "writeint");
        }

        private static void WriteLineStat()
        {
            EmitInstruction("call", "writeln");
        }
        private static void ReadIntegerStat(string dest)
        {
            EmitInstruction("call", "readint");
            EmitInstruction("mov", dest, "bx");
        }

        private static void CopyStat(string dest, string source)
        {
            EmitInstruction("mov", "ax", source);
            EmitInstruction("mov", dest, "ax");
        }

        private static void ReturnStat(string op)
        {
            EmitInstruction("mov", "ax", op);
        }

        private static void AssignFromReturnStat(string op)
        {
            EmitInstruction("mov", op, "ax");
        }

        private static void MulStat(string dest, string op1, string op2)
        {
            EmitInstruction("mov", "ax", op1);
            EmitInstruction("mov", "bx", op2);
            EmitInstruction("imul", "bx");
            EmitInstruction("mov", dest, "ax");
        }

        private static void DivStat(string dest, string op1, string op2)
        {
            EmitInstruction("mov", "ax", op1);
            EmitInstruction("cwd");
            EmitInstruction("mov", "bx", op2);
            EmitInstruction("idiv", "bx");
            EmitInstruction("mov", dest, "ax");
        }

        private static void AddStat(string dest, string op1, string op2)
        {
            EmitInstruction("mov", "ax", op1);
            EmitInstruction("add", "ax", op2);
            EmitInstruction("mov", dest, "ax");
        }

        private static void SubStat(string dest, string op1, string op2)
        {
            EmitInstruction("mov", "ax", op1);
            EmitInstruction("sub", "ax", op2);
            EmitInstruction("mov", dest, "ax");
        }

        private static void ArithmeticStat(string dest, string operand1, string @operator, string operand2)
        {
            switch (@operator)
            {
                case "+":
                    AddStat(dest, operand1, operand2);
                    break;
                case "-":
                    SubStat(dest, operand1, operand2);
                    break;
                case "*":
                    MulStat(dest, operand1, operand2);
                    break;
                case "/":
                    DivStat(dest, operand1, operand2);
                    break;
            }
        }

        private static void EmitInstruction(string instruction, string op1 = "", string op2 = "")
        {
            if (!string.IsNullOrEmpty(op1) && op1.Contains("_bp"))
            {
                op1 = ConvertTACBPOffset(op1);
            }

            if (!string.IsNullOrEmpty(op2))
            {
                if (op2.Contains("_bp"))
                {
                    op2 = ConvertTACBPOffset(op2);
                }

                op2 = ", " + op2;
            }

            Emit($"{instruction} {op1}{op2}");
        }

        private static void Emit(string code)
        {
            code += '\n';
            assemblyFile.Write(Encoding.ASCII.GetBytes(code));
        }

        private static string[] ReadLine()
        {
            var line = new List<string>();
            var currentTACStatement = string.Empty;

            while (true)
            {
                var ch = (char)ilFileReader.Read();
                if (char.IsWhiteSpace(ch))
                {
                    if (!string.IsNullOrEmpty(currentTACStatement))
                    {
                        line.Add(currentTACStatement);
                        currentTACStatement = string.Empty;
                    }

                    if (line.Count > 0 && ch == '\n')
                    {
                        break;
                    }
                }
                else
                {
                    currentTACStatement += char.ToLower(ch);
                }
            }

            return line.ToArray();
        }

        private static string ConvertTACBPOffset(string tacBPOffset)
        {
            // remove leading underscore and add square brackets
            return $"[{tacBPOffset.Trim('_')}]";
        }
    }
}

public class AssemblerException: Exception { }
