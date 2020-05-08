using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    public enum VarType { unknownType, intType, floatType, booleanType, voidType };

    public interface IStorable
    {
        int Offset { get; set; }
    }

    public class TableEntry
    {
        public override string ToString()
        {
            return Lexeme;
        }

        public Symbol Token { get; set; }
        public string Lexeme { get; set; }
        public int Depth { get; set; }
    }

    public class VarEntry: TableEntry, IStorable
    {
        public VarType TypeOfVariable { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }
    }

    public class ConstEntry<TValue>: TableEntry, IStorable
    {
        public VarType TypeOfConstant { get; set; }
        public int Offset { get; set; }
        public TValue Value { get; set; }
    }

    public class MethodEntry: TableEntry
    {
        public int SizeOfLocals { get; set; } = 0;
        public int SizeOfParameters { get; set; } = 0;
        public int NumberOfParameters
        {
            get
            {
                return ParamList.Count;
            }
        }
        public VarType ReturnType { get; set; }
        public List<(VarType, string)> ParamList { get; set; } = new List<(VarType, string)>();
    }

    public class ClassEntry: TableEntry
    {
        public int SizeOfLocals { get; set; }
        public List<string> MethodNames { get; set; } = new List<string>();
        public List<string> VariableNames { get; set; } = new List<string>();

        public bool HasMethod(string methodName)
        {
            return MethodNames.Any(name => name == methodName);
        }
    }

    public class LiteralEntry: TableEntry
    {
        public string Literal { get; set; }
    }

    public class SymbolTable
    {
        private readonly LinkedList<TableEntry>[] activeTable;
        private LinkedList<TableEntry>[] savedTable;
        private readonly int size;

        public bool UseSavedTable { get; set; } = false;

        public SymbolTable(int size)
        {
            activeTable = new LinkedList<TableEntry>[size];

            // retains all inserts made even when delete depth is used
            savedTable = new LinkedList<TableEntry>[size];
            this.size = size;
        }

        public TEntry Insert<TEntry>(string lexeme, Symbol token, int depth) where TEntry: TableEntry, new()
        {
            var duplicate = Lookup(lexeme);
            if (duplicate != null && duplicate.Depth == depth)
                throw new DuplicateLexemeException(lexeme);

            var hashAddress = Hash(lexeme);

            if (activeTable[hashAddress] == null)
                activeTable[hashAddress] = new LinkedList<TableEntry>();

            if (savedTable[hashAddress] == null)
                savedTable[hashAddress] = new LinkedList<TableEntry>();

            var value = new TEntry
            {
                Lexeme = lexeme,
                Token = token,
                Depth = depth
            };

            var node = activeTable[hashAddress].AddFirst(value);
            savedTable[hashAddress].AddFirst(value);
            return node.Value as TEntry;
        }

        public TEntry Lookup<TEntry>(string lexeme) where TEntry: TableEntry
        {
            var hashAddress = Hash(lexeme);
            var table = UseSavedTable ? savedTable : activeTable;
            var node = table[hashAddress]?.First;

            while (node != null)
            {
                if (node.Value.Lexeme == lexeme && node.Value is TEntry)
                {
                    return node.Value as TEntry;
                }

                node = node.Next;
            }

            return null;
        }

        public TableEntry Lookup(string lexeme)
        {
            var varLookup = Lookup<VarEntry>(lexeme);
            var classLookup = Lookup<ClassEntry>(lexeme);
            var methodLookup = Lookup<MethodEntry>(lexeme);
            var defaultLookup = Lookup<TableEntry>(lexeme);

            // variable, class, and method names might conflict so there needs to be precedence:
            return varLookup ?? classLookup ?? methodLookup ?? defaultLookup;

        }

        public void DeleteDepth(int depth)
        {
            for (int i = 0; i < size; i++)
            {
                var node = activeTable[i]?.First;

                while (node != null)
                {
                    if (node.Value.Depth == depth)
                    {
                        activeTable[i].Remove(node);
                    }

                    node = node.Next;
                }
            }
        }

        public void WriteTable(int depth)
        {
            for (int i = 0; i < size; i++)
            {
                var node = activeTable[i]?.First;

                while (node != null)
                {
                    if (node.Value.Depth == depth)
                    {
                        Console.WriteLine($"{node.Value.Lexeme} at depth {node.Value.Depth} of type {node.Value.GetType().Name}");
                    }

                    node = node.Next;
                }
            }
        }

        // credit: https://www.geeksforgeeks.org/hash-function-for-string-data-in-c-sharp/
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

    public class DuplicateLexemeException: Exception
    {
        public string Lexeme { get; private set; }

        public DuplicateLexemeException(string lexeme): base()
        {
            Lexeme = lexeme;
        }
    }
}
