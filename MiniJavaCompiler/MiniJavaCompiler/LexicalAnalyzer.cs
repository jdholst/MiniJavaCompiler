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
        private Symbol token;
        private string lexeme;
        private char ch;
        private int chIndex = 0;
        private int lineNo = 0;
        private int value;
        private double valueR;

        private string program;

        // reserve word lookup table
        private Dictionary<string, Symbol> resWords =
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
            };
        // symbol lookup table
        private Dictionary<string, Symbol> symbols =
            new Dictionary<string, Symbol>
            {
                { "+", Symbol.addopt },
                { "-", Symbol.addopt },
                { "||", Symbol.addopt },
                { "*", Symbol.mulopt },
                { "/", Symbol.mulopt },
                { "&&", Symbol.mulopt },
                { "&&", Symbol.mulopt },
                { "=", Symbol.assignopt },
                { "==", Symbol.relopt },
                { "!=", Symbol.relopt },
                { "<", Symbol.relopt },
                { "<=", Symbol.relopt },
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
            program = "class LexicalAnalyzer { };";
        }

        public void GetNextToken()
        {
            while (char.IsWhiteSpace(ch))
                GetNextCh();
            ProcessToken();
        }

        private void ProcessToken()
        {
            if (char.IsLetter(ch))
            {
                ProcessWordToken();
            }
            else if (char.IsDigit(ch))
            {
                ProcessNumToken();
            }
            else
            {
                ProcessSymToken();
            }
        }

        private void ProcessWordToken()
        {
            try
            {
                token = resWords[lexeme];
            }
            catch (KeyNotFoundException)
            {
                token = Symbol.idt;
            }
        }

        private void ProcessNumToken()
        {
            token = Symbol.numt;
        }

        private void ProcessSymToken()
        {
            try
            {
                token = symbols[lexeme];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception($"Symbol {lexeme} not found in lookup table");
            }

        }

        private void GetNextLexeme()
        {
            lexeme = "";
            while (!char.IsWhiteSpace(ch))
            {
                lexeme += ch;
                GetNextCh();
            }
        }

        private void GetNextCh()
        {
            ch = program[chIndex++];
        }
    }
}
