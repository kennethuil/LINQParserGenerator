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
            _expressionHelper = new ExpressionHelper();
            _regexNFABuilder = new RegexCharNFABuilder(_expressionHelper);
            var expr = _regexNFABuilder.CreateRegexParser("TestRegexCompile");
            _regexCompiler = expr.Compile();
        }

        // Symbols and publicly manipulable rules.
        Terminal<char, string> Identifier = new Terminal<char, string> { Name = "Identifier", InitialState = _regexCompiler(@"[A-Za-z][A-Za-z0-9]*") };
        Terminal<char, int> IntLiteral = new Terminal<char, int> {Name = "IntLiteral", InitialState = _regexCompiler(@"\d+"),
            StringAction = (l)=>int.Parse(l)};
        Terminal<char, double> FloatingLiteral = new Terminal<char, double> { Name = "FloatLiteral", InitialState = _regexCompiler(@"\d+\.\d+"),
            StringAction = (s)=>double.Parse(s)};
        Terminal<char> Plus = new Terminal<char> { Name = "Plus", InitialState = TerminalClassifier<char>.GetLiteralMatcher("+") };
        Terminal<char> Minus = new Terminal<char> { Name = "Minus", InitialState = TerminalClassifier<char>.GetLiteralMatcher("-") };
        Terminal<char> Star = new Terminal<char> { Name = "Star", InitialState = TerminalClassifier<char>.GetLiteralMatcher("*") };
        Terminal<char> Slash = new Terminal<char> { Name = "Slash", InitialState = TerminalClassifier<char>.GetLiteralMatcher("/") };
        Terminal<char> Percent = TerminalClassifier<char>.GetLiteralToken("Percent", "%");
        Terminal<char> Bang = TerminalClassifier<char>.GetLiteralToken("Bang", "!");
        Terminal<char> Caret = TerminalClassifier<char>.GetLiteralToken("Caret", "^");
        Terminal<char> Equal = TerminalClassifier<char>.GetLiteralToken("Equal", "=");
        Terminal<char> LeftParen = new Terminal<char> { Name = "LeftParen", InitialState = TerminalClassifier<char>.GetLiteralMatcher("(") };
        Terminal<char> RightParen = new Terminal<char> { Name = "RightParen", InitialState = TerminalClassifier<char>.GetLiteralMatcher(")") };
        Terminal<char> Comma = new Terminal<char> { Name = "Comma", InitialState = TerminalClassifier<char>.GetLiteralMatcher(",") };
        Terminal<char> Dot = TerminalClassifier<char>.GetLiteralToken("Dot", ".");
        Terminal<char> LeftBracket = new Terminal<char> { Name = "LeftBracket", InitialState = TerminalClassifier<char>.GetLiteralMatcher("[") };
        Terminal<char> RightBracket = TerminalClassifier<char>.GetLiteralToken("RightBracket", "]");
        Terminal<char> LeftAngleBracket = TerminalClassifier<char>.GetLiteralToken("LeftAngleBracket", "<");
        Terminal<char> RightAngleBracket = TerminalClassifier<char>.GetLiteralToken("RightAngleBracket", ">");
        Terminal<char> LeftBrace = TerminalClassifier<char>.GetLiteralToken("LeftBrace", "{");
        Terminal<char> RightBrace = TerminalClassifier<char>.GetLiteralToken("RightBrace", "}");
        Terminal<char> Ampersand = TerminalClassifier<char>.GetLiteralToken("BitwiseAnd", "&");
        Terminal<char> Pipe = TerminalClassifier<char>.GetLiteralToken("BitwiseOr", "|");
        Terminal<char> BooleanAnd = TerminalClassifier<char>.GetLiteralToken("BooleanAnd", "&&");
        Terminal<char> BooleanOr = TerminalClassifier<char>.GetLiteralToken("BooleanOr", "||");

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


        public LanguageGrammar()
        {
            var rules = new GrammarRule[] {
                GrammarRule<TValue, TValue>.Create(Expression, AddExp),

                IntLiteralRule = GrammarRule<int, TValue>.Create(PrimaryExp, IntLiteral),
                DoubleLiteralRule = GrammarRule<double, TValue>.Create(PrimaryExp, FloatingLiteral),
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
