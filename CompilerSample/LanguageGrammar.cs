using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.CodeGen.Expressions;
using Framework.Parsing;

namespace CompilerSample
{
    public class LanguageGrammar<TValue> : Grammar<char>
    {
        // Boilerplate stuff to set up the regex compiler.
        // TODO: Package this up somewhere.
        static Func<string, FiniteAutomatonState<char>> _regexCompiler;
        static RegexCharNFABuilder _regexNFABuilder;
        static ExpressionHelper _expressionHelper;
        static LanguageGrammar()
        {
            _regexCompiler = RegexCharNFABuilder.RegexCompiler;
        }

        // Symbols and publicly manipulable rules.


        private static FiniteAutomatonState<char> IdentifierFSM()
        {
            FiniteAutomatonState<char> second;
            return new StateBuilder<char>()
                .OnAnyOf(Utilities.AllLetters())
                .GotoNew(out second)
                .Accept()
                .OnAnyOf(Utilities.AllLetters(), Utilities.AllDigits())
                .Goto(second)
                .InitialState;
        }

        private static FiniteAutomatonState<char> IntLiteralFSM()
        {
            FiniteAutomatonState<char> next;
            return new StateBuilder<char>()
                .OnAnyOf(Utilities.AllDigits())
                .GotoNew(out next)
                .Accept()
                .OnAnyOf(Utilities.AllDigits())
                .Goto(next)
                .InitialState;
        }

        private static FiniteAutomatonState<char> FloatLiteralFSM()
        {
            FiniteAutomatonState<char> nextInt;
            FiniteAutomatonState<char> nextDecimal;
            return new StateBuilder<char>()
                .OnAnyOf(Utilities.AllDigits()).GotoNew(out nextInt)
                .OnAnyOf(Utilities.AllDigits()).Goto(nextInt)
                .OnAnyOf('.').GotoNew()
                .OnAnyOf(Utilities.AllDigits()).GotoNew(out nextDecimal)
                .Accept()
                .OnAnyOf(Utilities.AllDigits()).Goto(nextDecimal)
                .InitialState;
        }

        private static FiniteAutomatonState<char> QuotedStringFSM()
        {
            FiniteAutomatonState<char> inner;
            return new StateBuilder<char>()
                .OnAnyOf('\"').GotoNew(out inner)
                .OnAnyExcept('\"', '\\').Goto(inner)
                .OnAnyOf('\\').GotoNew().OnAnything().Goto(inner)
                .OnAnyOf('\"').GotoNew().Accept().InitialState;

        }

        //Terminal<char, string> Identifier = new Terminal<char, string> { Name = "Identifier", InitialState = _regexCompiler(@"[A-Za-z][A-Za-z0-9]*") };
        Terminal<char, string> Identifier = new Terminal<char, string>
        {
            Name = "Identifier",
            InitialState = IdentifierFSM()
        };

        public static string QuotedString(string qstring)
        {
            qstring = qstring.Substring(1, qstring.Length - 2);
            StringBuilder result = new StringBuilder();
            int i;
            for (i = 0; i < qstring.Length; ++i)
            {
                var ch = qstring[i];
                if (ch == '\\')
                {
                    ++i;
                    char translated;
                    switch (qstring[i])
                    {
                        case '0':
                            translated = '\0';
                            break;
                        case 'a':
                            translated = '\a';
                            break;
                        case 'b':
                            translated = '\b';
                            break;
                        case 'f':
                            translated = '\f';
                            break;
                        case 'n':
                            translated = '\n';
                            break;
                        case 'r':
                            translated = '\r';
                            break;
                        case 't':
                            translated = '\t';
                            break;
                        case 'v':
                            translated = '\v';
                            break;
                        default:
                            translated = qstring[i];
                            break;
                    };
                    result.Append(translated);
                    continue;
                }
                result.Append(ch);
            }
            return result.ToString();
        }

        Terminal<char, int> IntLiteral = new Terminal<char, int>
        {
            Name = "IntLiteral",
            InitialState = IntLiteralFSM(),
            StringAction = (l)=>int.Parse(l)};
        Terminal<char, double> FloatingLiteral = new Terminal<char, double> { Name = "FloatLiteral", InitialState = FloatLiteralFSM(),
            StringAction = (s)=>double.Parse(s)};
        Terminal<char, string> StringLiteral = new Terminal<char, string> { Name = "StringLiteral", InitialState = QuotedStringFSM(),
            StringAction = (s)=>QuotedString(s)};
        Terminal<char> Plus = new Terminal<char> { Name = "+", InitialState = TerminalClassifier<char>.GetLiteralMatcher("+") };
        Terminal<char> Minus = new Terminal<char> { Name = "-", InitialState = TerminalClassifier<char>.GetLiteralMatcher("-") };
        Terminal<char> Star = new Terminal<char> { Name = "*", InitialState = TerminalClassifier<char>.GetLiteralMatcher("*") };
        Terminal<char> Slash = new Terminal<char> { Name = "/", InitialState = TerminalClassifier<char>.GetLiteralMatcher("/") };
        Terminal<char> Percent = TerminalClassifier<char>.GetLiteralToken("%", "%");
        Terminal<char> Bang = TerminalClassifier<char>.GetLiteralToken("!", "!");
        Terminal<char> Caret = TerminalClassifier<char>.GetLiteralToken("^", "^");
        Terminal<char> Equal = TerminalClassifier<char>.GetLiteralToken("=", "=");
        Terminal<char> LeftParen = new Terminal<char> { Name = "(", InitialState = TerminalClassifier<char>.GetLiteralMatcher("(") };
        Terminal<char> RightParen = new Terminal<char> { Name = ")", InitialState = TerminalClassifier<char>.GetLiteralMatcher(")") };
        Terminal<char> Comma = new Terminal<char> { Name = ",", InitialState = TerminalClassifier<char>.GetLiteralMatcher(",") };
        Terminal<char> Dot = TerminalClassifier<char>.GetLiteralToken(".", ".");
        Terminal<char> LeftBracket = new Terminal<char> { Name = "[", InitialState = TerminalClassifier<char>.GetLiteralMatcher("[") };
        Terminal<char> RightBracket = TerminalClassifier<char>.GetLiteralToken("]", "]");
        Terminal<char> LeftAngleBracket = TerminalClassifier<char>.GetLiteralToken("<", "<");
        Terminal<char> RightAngleBracket = TerminalClassifier<char>.GetLiteralToken(">", ">");
        Terminal<char> LeftBrace = TerminalClassifier<char>.GetLiteralToken("{", "{");
        Terminal<char> RightBrace = TerminalClassifier<char>.GetLiteralToken("}", "}");
        Terminal<char> Ampersand = TerminalClassifier<char>.GetLiteralToken("&", "&");
        Terminal<char> Pipe = TerminalClassifier<char>.GetLiteralToken("|", "|");
        Terminal<char> BooleanAnd = TerminalClassifier<char>.GetLiteralToken("&&", "&&");
        Terminal<char> BooleanOr = TerminalClassifier<char>.GetLiteralToken("||", "||");

        NonTerminal<TValue> PrimaryExp = new NonTerminal<TValue>("PrimaryExp");
        NonTerminal<TValue> MultExp = new NonTerminal<TValue>("MultExp");
        NonTerminal<TValue> AddExp = new NonTerminal<TValue>("AddExp");
        NonTerminal<TValue> Expression = new NonTerminal<TValue>("Expression");

        public readonly GrammarRule<string, TValue> VariableRefRule;
        public readonly GrammarRule<int, TValue> IntLiteralRule;
        public readonly GrammarRule<double, TValue> DoubleLiteralRule;
        public readonly GrammarRule<TValue, TValue, TValue> PlusRule;
        public readonly GrammarRule<TValue, TValue, TValue> MinusRule;
        public readonly GrammarRule<TValue, TValue, TValue> MultiplyRule;
        public readonly GrammarRule<TValue, TValue, TValue> DivideRule;
        public readonly GrammarRule<TValue, TValue, TValue> ModRule;
        public readonly GrammarRule<TValue, TValue> NegateRule;
        public readonly GrammarRule<string, TValue> StringLiteralRule;


        public LanguageGrammar()
        {
            var rules = new GrammarRule[] {
                GrammarRule<TValue, TValue>.Create(Expression, AddExp),

                IntLiteralRule = GrammarRule<int, TValue>.Create(PrimaryExp, IntLiteral),
                DoubleLiteralRule = GrammarRule<double, TValue>.Create(PrimaryExp, FloatingLiteral),
                StringLiteralRule = GrammarRule<string, TValue>.Create(PrimaryExp, StringLiteral),
                VariableRefRule = GrammarRule<string, TValue>.Create(PrimaryExp, Identifier),
                NegateRule = GrammarRule<TValue, TValue>.Create(PrimaryExp, Minus, PrimaryExp),
                GrammarRule<TValue, TValue>.Create(PrimaryExp, LeftParen, Expression, RightParen),

                MultiplyRule = GrammarRule<TValue, TValue, TValue>.Create(MultExp, MultExp, Star, PrimaryExp),
                DivideRule = GrammarRule<TValue, TValue, TValue>.Create(MultExp, MultExp, Slash, PrimaryExp),
                ModRule = GrammarRule<TValue, TValue, TValue>.Create(MultExp, MultExp, Percent, PrimaryExp),
                GrammarRule<TValue, TValue>.Create(MultExp, PrimaryExp),

                PlusRule = GrammarRule<TValue, TValue, TValue>.Create(AddExp, AddExp, Plus, MultExp),
                MinusRule = GrammarRule<TValue, TValue, TValue>.Create(AddExp, AddExp, Minus, MultExp),
                GrammarRule<TValue, TValue>.Create(AddExp, MultExp),
            };
            Init(rules);
        }
    }
}
