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
        arrayt,
        commat,
        semit,
        periodt,
        quotet,
        idt,
        numt,
        eoft
    }

    public class LexicalAnalyzer
    {
        public Symbol Token
        {
            get
            {
                return token;
            }
        }

        private Symbol token;
        private string lexeme;
        private char ch = ' ';
        private int chIndex = 0;
        private int lineNo = 0;
        private int value;
        private double valueR;

        private string[] program;

        // token lookup table
        private Dictionary<string, Symbol> tokenSymbols =
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
                { "printt", Symbol.printt },
                { "lengtht", Symbol.lengtht },
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
                { "[", Symbol.arrayt },
                { "]", Symbol.arrayt },
                { ",", Symbol.commat },
                { ";", Symbol.semit },
                { ".", Symbol.periodt },
                { "\"", Symbol.quotet }
            };

        public LexicalAnalyzer()
        {
            program = new string[] 
            {
                "class LexicalAnalyzer {",
                "};"
            };
        }

        public void GetNextToken()
        {
            lexeme = "";
            while (char.IsWhiteSpace(ch))
                GetNextCh();
            ProcessToken();
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

            // attempt to read as res word
            try
            {
                ReadToken();
            }
            catch (KeyNotFoundException)
            {
                // is a idt if not res word
                token = Symbol.idt;
            }
        }

        private void ProcessNumToken()
        {
            ReadRest(() => char.IsDigit(ch));
            if (ch == '.')
            {
                lexeme += ch;
                GetNextCh();
                if (!char.IsDigit(ch))
                {
                    throw new Exception($"Invalid num symbol {lexeme} at line {lineNo} col {chIndex}");
                }
                ReadRest(() => char.IsDigit(ch));
                valueR = double.Parse(lexeme);
            }
            else
            {
                value = int.Parse(lexeme);
            }

            token = Symbol.numt;
        }

        private void ProcessSingleToken()
        {
            ReadToken();
        }

        private void ProcessDoubleToken()
        {
            ReadNextCh();
            ReadToken();
        }

        private void GetNextCh()
        {
            if (lineNo >= program.Length)
            {
                if (chIndex >= program[lineNo].Length)
                {
                    chIndex = 0;
                    lineNo++;
                }
                ch = program[lineNo][chIndex++];
            }
        }

        private void ReadNextCh()
        {
            lexeme += ch;
            GetNextCh();
        }

        private void ReadRest(Func<bool> condition)
        {
            do
            {
                ReadNextCh();
            } while (condition());
        }

        private void ReadToken()
        {
            try
            {
                token = tokenSymbols[lexeme];
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException($"Symbol {lexeme} not found in lookup table");
            }
        }
    }
}
