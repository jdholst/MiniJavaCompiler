using System;
using System.Collections.Generic;
using System.Text;

namespace MiniJavaCompiler
{
    public enum VarType { intType, floatType, booleanType, voidType };
    public enum EntryType { constEntry, varEntry, methodEntry, classEntry };

    public class TableEntry
    {
        public Symbol Token { get; set; }
        public string Lexeme { get; set; }
        public int Depth { get; set; }
        public EntryType TypeOfEntry { get; set; }
    }

    public class VarEntry: TableEntry
    {
        public VarType TypeOfVariable { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }
    }

    public class ConstEntry<TValue>: TableEntry
    {
        public VarType TypeOfConstant { get; set; }
        public int Offset { get; set; }
        public TValue Value { get; set; }
    }

    public class MethodEntr: TableEntry
    {
        public int SizeOfLocals { get; set; }
        public int NumberOfParameters { get; set; }
        public VarType ReturnType { get; set; }
        public List<VarType> ParamList { get; set; }
    }

    public class ClassEntry: TableEntry
    {
        public int SizeOfLocals { get; set; }
        public List<string> MethodNames { get; set; }
        public List<string> VariableNames { get; set; }
    }

    public class SymbolTable
    {
        private LinkedList<TableEntry>[] table;
        private int size;

        public SymbolTable(int size)
        {
            table = new LinkedList<TableEntry>[size];

            this.size = size;
        }

        public void Insert(string lexeme, Symbol token, int depth)
        {
            var hashAddress = Hash(lexeme);

            table[hashAddress] = new LinkedList<TableEntry>();
            table[hashAddress].AddFirst(new TableEntry
            {
                Lexeme = lexeme,
                Token = token,
                Depth = depth
            });
        }

        public TableEntry Lookup(string lexeme)
        {
            var hashAddress = Hash(lexeme);
            var node = table[hashAddress]?.First;

            while (node != null)
            {
                if (node.Value.Lexeme == lexeme)
                {
                    return node.Value;
                }

                node = node.Next;
            }

            return null;
        }

        public void DeleteDepth(int depth)
        {
            for (int i = 0; i < size; i++)
            {
                var node = table[i]?.First;

                while (node != null)
                {
                    if (node.Value.Depth == depth)
                    {
                        table[i].Remove(node);
                    }

                    node = node.Next;
                }
            }
        }

        public void WriteTable(int depth)
        {
            for (int i = 0; i < size; i++)
            {
                var node = table[i]?.First;

                while (node != null)
                {
                    if (node.Value.Depth == depth)
                    {
                        Console.WriteLine(node.Value.Lexeme);
                    }

                    node = node.Next;
                }
            }
        }

        // credit to person I stole from: https://www.geeksforgeeks.org/hash-function-for-string-data-in-c-sharp/
        private int Hash(string lexeme)
        {
            long total = 0;
            char[] c;
            c = lexeme.ToCharArray();

            // Horner's rule for generating a polynomial  
            // of 11 using ASCII values of the characters 
            for (int k = 0; k <= c.GetUpperBound(0); k++)
                total += 11 * total + (int)c[k];

            total = total % size;

            if (total < 0)
                total += size;

            return (int)total;
        }
    }
}
