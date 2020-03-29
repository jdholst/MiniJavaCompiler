﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MiniJavaCompiler
{
    public enum VarType { intType, floatType, booleanType, voidType };

    public class TableEntry
    {
        public Symbol Token { get; set; }
        public string Lexeme { get; set; }
        public int Depth { get; set; }
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
        public List<VarType> ParamList { get; set; } = new List<VarType>();
    }

    public class ClassEntry: TableEntry
    {
        public int SizeOfLocals { get; set; }
        public List<string> MethodNames { get; set; } = new List<string>();
        public List<string> VariableNames { get; set; } = new List<string>();
    }

    public class SymbolTable
    {
        private readonly LinkedList<TableEntry>[] table;
        private readonly int size;

        public SymbolTable(int size)
        {
            table = new LinkedList<TableEntry>[size];
            this.size = size;
        }

        public void Insert<TEntry>(string lexeme, Symbol token, int depth) where TEntry: TableEntry, new()
        {
            var duplicate = Lookup(lexeme);
            if (duplicate != null && duplicate.Depth == depth)
                throw new DuplicateLexemeException(lexeme);

            var hashAddress = Hash(lexeme);

            if (table[hashAddress] == null)
                table[hashAddress] = new LinkedList<TableEntry>();

            table[hashAddress].AddFirst(new TEntry
            {
                Lexeme = lexeme,
                Token = token,
                Depth = depth
            });
        }

        public TEntry Lookup<TEntry>(string lexeme) where TEntry: TableEntry
        {
            var hashAddress = Hash(lexeme);
            var node = table[hashAddress]?.First;

            while (node != null)
            {
                if (node.Value.Lexeme == lexeme)
                {
                    return node.Value as TEntry;
                }

                node = node.Next;
            }

            return null;
        }

        public TableEntry Lookup(string lexeme)
        {
            return Lookup<TableEntry>(lexeme);
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