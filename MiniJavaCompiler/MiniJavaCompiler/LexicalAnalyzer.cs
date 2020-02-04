using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniJavaCompiler
{
    public enum Symbol
    {
        classt,
        publict,
        statict,
        voidt,
        maint,
        stringt,
        extendst,
        returnt,
        intt,
        booleant,
        ift,
        elset,
        whilet,
        printt,
        lengtht,
        truet,
        falset,
        thist,
        newt,
        relopt,
        addopt,
        mulopt,
        assignopt,
        lparent,
        rparent,
        begint,
        endt,
        rarrayt,
        larrayt,
        commat,
        semit,
        periodt,
        quotet,
        idt,
        numt,
        unkownt,
        eoft
    }

    public class LexicalAnalyzer
    {
        public Symbol Token { get; private set; }
        public string Literal { get; private set; }
        public int Value { get; private set; }
        public double ValueR { get; private set; }

        private string lexeme;
        private char ch = ' ';

        private bool endOfLiteral = false;

        private StreamReader programReader;
        private bool eof = false;

        // token lookup table
        private readonly Dictionary<string, Symbol> tokenSymbols =
            new Dictionary<string, Symbol>
            {
                { "class", Symbol.classt },
                { "public", Symbol.publict },
                { "void", Symbol.voidt },
                { "main", Symbol.maint },
                { "String", Symbol.stringt },
                { "extends", Symbol.extendst },
                { "return", Symbol.returnt },
                { "int", Symbol.intt },
                { "else", Symbol.elset },
                { "while", Symbol.whilet  },
                { "System.out.println", Symbol.printt },
                { "length", Symbol.lengtht },
                { "true", Symbol.truet },
                { "false", Symbol.falset },
                { "this", Symbol.thist },
                { "new", Symbol.newt },
                { "+", Symbol.addopt },
                { "-", Symbol.addopt },
                { "||", Symbol.addopt },
                { "*", Symbol.mulopt },
                { "/", Symbol.mulopt },
                { "&&", Symbol.mulopt },
                { "=", Symbol.assignopt },
                { "==", Symbol.relopt },
                { "!=", Symbol.relopt },
                { "<", Symbol.relopt },
                { ">", Symbol.relopt },
                { ">=", Symbol.relopt },
                { "(", Symbol.lparent },
                { ")", Symbol.rparent },
                { "{", Symbol.begint },
                { "}", Symbol.endt },
                { "[", Symbol.larrayt },
                { "]", Symbol.rarrayt },
                { ",", Symbol.commat },
                { ";", Symbol.semit },
                { ".", Symbol.periodt },
                { "\"", Symbol.quotet }
            };

        public LexicalAnalyzer(string programPath)
        {
            this.programReader = File.OpenText(programPath);
        }

        public void GetAllTokensAndDisplay()
        {
            while (!eof)
            {
                GetNextToken();
                Console.Write($"{lexeme}: {Token}");

                if (Token == Symbol.numt)
                {
                    Console.Write($" with Value: {Value} ValueR: {ValueR}");
                }
                else if (Token == Symbol.quotet && endOfLiteral)
                {
                    Console.Write($" with Literal: {Literal}");
                }
                Console.WriteLine();
            }
        }

        public void GetNextToken()
        {
            Value = 0;
            ValueR = 0;
            Literal = "";
            lexeme = "";

            SkipWhiteSpaces();

            try
            {
                if (!programReader.EndOfStream)
                {
                    ProcessToken();
                }
                else
                {
                    programReader.Close(); // close file
                    eof = true;
                    Token = Symbol.eoft;
                }
            }
            catch (LexicalAnalyzerException)
            {
                // if any error occurs set to unknown
                Token = Symbol.unkownt;
            }
        }

        private void ProcessToken()
        {
            ReadNextCh();

            if (char.IsLetter(lexeme[0]))
            {
                ProcessWordToken();
            }
            else if (char.IsDigit(lexeme[0]))
            {
                ProcessNumToken();
            }
            else if (
                (lexeme[0] == '>' || lexeme[0] == '<' || lexeme[0] == '='  || lexeme[0] == '!') && ch == '=' // relop double token
                || (lexeme[0] == '&' || lexeme[0] == '|') && (ch == '&' || ch == '|')) // addop/mulop double token
            {
                ProcessDoubleToken();
            }
            else if (lexeme[0] == '/' && ch == '/')
            {
                ProcessSingleLineComment();
            }
            else if (lexeme[0] == '/' && ch == '*')
            {
                ProcessMultiLineComment();
            }
            else
            {
                ProcessSingleToken();
            }
        }

        private void ProcessWordToken()
        {
            ReadUntil(() => char.IsLetterOrDigit(ch) || ch == '_');

            if (lexeme == "System" && ch == '.')
            {
                ReadNextCh();
                ReadUntil(() => char.IsLetterOrDigit(ch));
                if (lexeme == "System.out" && ch == '.')
                {
                    ReadNextCh();
                    ReadUntil(() => char.IsLetterOrDigit(ch));
                }
            }

            // attempt to read as res word
            try
            {
                ReadToken();
            }
            catch (LexicalAnalyzerException)
            {
                // is a idt if not res word
                Token = Symbol.idt;
            }
        }

        private void ProcessNumToken()
        {
            ReadUntil(() => char.IsDigit(ch));
            if (ch == '.')
            {
                ReadDecimal();
            }
            else
            {
                Value = int.Parse(lexeme);
            }

            Token = Symbol.numt;
        }

        private void ProcessSingleToken()
        {
            ReadToken();
            if (Token == Symbol.quotet)
            {
                if (!endOfLiteral)
                {
                    ReadLiteral();
                }
                else
                {
                    endOfLiteral = false;
                }
            }
        }

        private void ProcessDoubleToken()
        {
            ReadNextCh();
            ReadToken();
        }

        private void ProcessSingleLineComment()
        {
            programReader.ReadLine(); // read and ignore
            GetNextCh();
            GetNextToken();
        }

        private void ProcessMultiLineComment()
        {
            while (!programReader.EndOfStream)
            {
                GetNextCh();
                if (ch == '*')
                {
                    GetNextCh();
                    if (ch == '/')
                    {
                        break;
                    }
                }
            }
            GetNextCh();
            GetNextToken();
        }

        private void GetNextCh()
        {
            if (!programReader.EndOfStream)
            {
                ch = (char)programReader.Read();
            }
        }

        private void ReadNextCh()
        {
            lexeme += ch;
            GetNextCh();
        }

        private void ReadUntil(Func<bool> condition)
        {
            while (condition() && !programReader.EndOfStream)
            {
                ReadNextCh();
            }
        }

        private void ReadDecimal()
        {
            ReadNextCh(); // read .
            if (!char.IsDigit(ch))
            {
                // must have numbers after decimal point
                throw new LexicalAnalyzerException();
            }
            ReadUntil(() => char.IsDigit(ch));
            ValueR = double.Parse(lexeme);
        }

        private void ReadLiteral()
        {
            while (ch != '"')
            {
                Literal += ch;
                GetNextCh();
            }
            endOfLiteral = true;
        }

        private void ReadToken()
        {
            try
            {
                // check if token exist in lookup table
                Token = tokenSymbols[lexeme];
            }
            catch (KeyNotFoundException)
            {
                throw new LexicalAnalyzerException();
            }
        }

        private void SkipWhiteSpaces()
        {
            while (char.IsWhiteSpace(ch))
                GetNextCh();
        }
    }

    // thrown after encountering invalid tokens
    public class LexicalAnalyzerException: Exception
    {
    }
}
