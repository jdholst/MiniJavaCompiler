using System;
using System.Collections.Generic;
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
        private int chIndex = 0;
        private int lineNo = 0;

        private string[] program;
        private bool isEof = false;

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

        public LexicalAnalyzer(string[] program)
        {
            this.program = program;
        }

        public void GetAllTokensAndDisplay()
        {
            while (lineNo < program.Length)
            {
                GetNextToken();
                Console.WriteLine($"{lexeme}: {Token}");
            }
        }

        public void GetNextToken()
        {
            Value = 0;
            ValueR = 0;
            Literal = "";
            lexeme = "";

            while (char.IsWhiteSpace(ch))
                GetNextCh();

            if (!isEof)
            {
                ProcessToken();
            }
            else
            {
                Token = Symbol.eoft;
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
                ch == '=' && (lexeme[0] == '>' || lexeme[0] == '<' || lexeme[0] == '='  || lexeme[0] == '!') // relop double token
                || (ch == '&' || ch == '|') && (lexeme[0] == '&' || lexeme[0] == '|')) // addop/mulop double token
            {
                ProcessDoubleToken();
            }
            else
            {
                ProcessSingleToken();
            }
        }

        private void ProcessWordToken()
        {
            ReadRest(() => char.IsLetterOrDigit(ch) || ch == '_');

            if (lexeme == "System" && ch == '.')
            {
                ReadNextCh();
                ReadRest(() => char.IsLetterOrDigit(ch));
                if (lexeme == "System.out" && ch == '.')
                {
                    ReadNextCh();
                    ReadRest(() => char.IsLetterOrDigit(ch));
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
            ReadRest(() => char.IsDigit(ch));
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

            }
        }

        private void ProcessDoubleToken()
        {
            ReadNextCh();
            ReadToken();
        }

        private void GetNextCh()
        {
            if (lineNo < program.Length && chIndex >= program[lineNo].Length)
            {
                chIndex = 0;
                lineNo++;
            }

            if (lineNo < program.Length)
            {
                ch = program[lineNo][chIndex++];
            }
            else
            {
                isEof = true;
            }
        }

        private void ReadNextCh()
        {
            lexeme += ch;
            GetNextCh();
        }

        private void ReadRest(Func<bool> condition)
        {
            while (condition() && !isEof)
            {
                ReadNextCh();
            }
        }

        private void ReadDecimal()
        {
            ReadNextCh(); // read .
            if (!char.IsDigit(ch))
            {
                throw new LexicalAnalyzerException($"Invalid num symbol {lexeme} at line {lineNo} col {chIndex}");
            }
            ReadRest(() => char.IsDigit(ch));
            ValueR = double.Parse(lexeme);
        }

        private void ReadLiteral()
        {
            GetNextCh();
            // ReadRest(() => )
        }

        private void ReadToken()
        {
            try
            {
                Token = tokenSymbols[lexeme];
            }
            catch (KeyNotFoundException)
            {
                throw new LexicalAnalyzerException($"Lexeme {lexeme} not found in symbol lookup table");
            }
        }
    }

    public class LexicalAnalyzerException: Exception
    {
        public LexicalAnalyzerException(string message): base(message) { }
    }
}
