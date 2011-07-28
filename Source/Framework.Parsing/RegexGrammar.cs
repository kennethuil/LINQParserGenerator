using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Framework.Parsing
{
    public class RegexGrammar<TValue> : Grammar<char>
    {
        private static readonly Expression<Func<char, bool>> _matchBackslash =
            (c) => c == '\\';
        private static readonly Expression<Func<char, bool>> _matchSingleCharEscapeChar =
            (c) => !(c == 'w' ||
                            c == 's' || c == 'S' || c == 'd' || c == 'D');
        private static readonly Expression<Func<char,bool>> _matchCharClassEscapeChar =
            (c) => c == 'w' ||
                            c == 's' || c == 'S' || c == 'd' || c == 'D';
        private static readonly Expression<Func<char, bool>> _matchDigit =
            (c) => char.IsDigit(c);
        private static readonly Expression<Func<char, bool>> _matchHex =
            (c) => char.IsDigit(c) || c == 'a' || c == 'A' || c == 'b' || c == 'B' || c == 'c' ||
                c == 'C' || c == 'd' || c == 'D' || c == 'e' || c == 'E' || c == 'f' || c == 'F';
        private static readonly Expression<Func<char, bool>> _matchOpenBracket =
            (c) => c == '[';
        private static readonly Expression<Func<char, bool>> _matchCloseBracket =
            (c) => c == ']';
        private static readonly Expression<Func<char, bool>> _matchOpenBrace =
            (c) => c == '{';
        private static readonly Expression<Func<char, bool>> _matchCloseBrace =
            (c) => c == '}';
        private static readonly Expression<Func<char, bool>> _matchOpenParen =
            (c) => c == '(';
        private static readonly Expression<Func<char, bool>> _matchCloseParen =
            (c) => c == ')';
        private static readonly Expression<Func<char, bool>> _matchCaret =
            (c) => c == '^';
        private static readonly Expression<Func<char, bool>> _matchDash =
            (c) => c == '-';
        private static readonly Expression<Func<char, bool>> _matchDollar =
            (c) => c == '$';
        private static readonly Expression<Func<char, bool>> _matchQuestion =
            (c) => c == '?';
        private static readonly Expression<Func<char, bool>> _matchDot =
            (c) => c == '.';
        private static readonly Expression<Func<char, bool>> _matchColon =
            (c) => c == ':';
        private static readonly Expression<Func<char, bool>> _matchPlus =
            (c) => c == '+';
        private static readonly Expression<Func<char, bool>> _matchStar =
            (c) => c == '*';
        private static readonly Expression<Func<char,bool>> _matchPipe =
            (c)=>c == '|';
        private static readonly Expression<Func<char, bool>> _matchAny =
            (c) => true;
        private static readonly Expression<Func<char, bool>> _matchNonControlChar =
            (c) => c != '\"' && c != '\\' && c != '[' && c != ']' && c != '(' && c != ')' && c != '?' && c != '+' && c != '*' && c != '-' && c != '^' && c != '|'
            && c != '-';

        // An escape sequence that matches one specific character
        public Terminal<char, TValue> SingleCharEscape { get; private set; }

        // An escape sequence that matches any one of several characters (such as "\d" which matches any digit)
        public Terminal<char, TValue> CharClassEscape { get; private set; }

        // A single-character expression that is not an escape sequence
        public Terminal<char, TValue> OtherChar { get; private set; }

        // Any expression that matches one specific single character
        public NonTerminal<TValue> SpecificChar { get; private set; }

        // An expression that is a single literal character or a single escape sequence
        public NonTerminal<TValue> SimpleChar { get; private set; }

        // An expression that matches any one of a range of characters
        public NonTerminal<TValue> SelectRangeChar { get; private set; }

        // An expression that matches any one of an explicit list of characters
        public NonTerminal<TValue> SelectChar { get; private set; }

        // An expression that matches any one character that is not in the explicit list
        public NonTerminal<TValue> SelectNotChar { get; private set; }

        // Represent the explicit list of characters that is part of a selection
        public NonTerminal<IList<TValue>> CharList { get; private set; }

        // Any expression that matches a sequence of exactly one character, but it might match any one of several single-character sequences
        public NonTerminal<TValue> SingleChar { get; private set; }

        // An expression that either matches a single character or is a grouped (capturing or non-capturing) expression.
        public NonTerminal<TValue> SubExpr { get; private set; }

        // An expression with a quantifier applied
        public NonTerminal<TValue> QuantExpr { get; private set; }

        // An expression that concatenates two other expressions
        public NonTerminal<TValue> ConcatExpr { get; private set; }

        // A regular expression
        public NonTerminal<TValue> Expr { get; private set; }

        public GrammarRule<TValue, TValue> RegexRule { get; private set; }
        public GrammarRule<TValue, TValue, TValue> ConcatRule { get; private set; }
        public GrammarRule<TValue, TValue, TValue> AlternateRule { get; private set; }
        public GrammarRule<TValue, TValue> ZeroOrMoreRule { get; private set; }
        public GrammarRule<TValue, TValue> OneOrMoreRule { get; private set; }
        public GrammarRule<TValue, TValue> ZeroOrOneRule { get; private set; }
        public GrammarRule<TValue, TValue> CapturingGroupRule { get; private set; }
        public GrammarRule<IList<TValue>, TValue> SelectCharRule { get; private set; }
        public GrammarRule<IList<TValue>, TValue> SelectNotCharRule { get; private set; }
        public GrammarRule<TValue, TValue, TValue> SelectRangeCharRule { get; private set; }
        public GrammarRule<IList<TValue>,TValue, IList<TValue>> SelectCharListAppendRule { get; private set; }
        public GrammarRule<TValue, IList<TValue>> SelectCharListSingleonRule { get; private set; }

        static Terminal<char> MatchLiteral(string terminalName, string str)
        {
            return new Terminal<char>
            {
                InitialState = RegexCharNFABuilder.GetLiteralMatcher(str),
                Name = terminalName
            };
        }

        static Terminal<char, TValue> MatchCapturingCharSequence(string terminalName, 
            params Expression<Func<char, bool>>[] charMatches)
        {
            //FiniteAutomatonState<char>[] states = GetCharSequenceStates(charMatches);
            var term = new Terminal<char, TValue>
            {
                InitialState = TerminalClassifier<char>.GetExpressionSequenceMatcher(charMatches),
                Name = terminalName
            };
            return term;
        }

        private static FiniteAutomatonState<char>[] GetCharSequenceStates(Expression<Func<char, bool>>[] charMatches)
        {
            var states = new FiniteAutomatonState<char>[charMatches.Length + 1];
            states[charMatches.Length] = new FiniteAutomatonState<char>
                                             {
                                                 IsAccepting = true
                                             };
            int i;
            for (i = charMatches.Length - 1; i >= 0; --i)
            {
                states[i] = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            CharacterMatchExpression = charMatches[i],
                            Target = states[i+1]
                        }
                    }
                };
            }
            return states;
        }

        // TODO: Inside of a [] or [^] construct, we need to take just about any character that's
        // not a ] literally.  Also, we need to support [a-bghyc-d]

        public RegexGrammar()
        {
            SingleCharEscape = MatchCapturingCharSequence("SingleCharEscape", _matchBackslash, _matchSingleCharEscapeChar);
            CharClassEscape = MatchCapturingCharSequence("CharClassEscape", _matchBackslash, _matchCharClassEscapeChar);
            // TODO: Hex & Unicode escape
            OtherChar = MatchCapturingCharSequence("OtherChar", _matchNonControlChar);

            var openNonCapturing = MatchLiteral("OpenNonCapturing", "(?:");
            var openParen = MatchLiteral("OpenParen", "(");
            var closeParen = MatchLiteral("CloseParen", ")");
            var openBracket = MatchLiteral("OpenBracket", "[");
            var openBracketCaret = MatchLiteral("OpenBracketCaret", "[^");
            var closeBracket = MatchLiteral("CloseBracket", "]");

            var star = MatchLiteral("Star", "*");
            var plus = MatchLiteral("Plus", "+");
            var question = MatchLiteral("Question", "?");
            var dash = MatchLiteral("Dash", "-");
            var pipe = MatchLiteral("Pipe", "|");

            // NonTerminals.
            SpecificChar = new NonTerminal<TValue> { Name = "SpecificChar" };
            SimpleChar = new NonTerminal<TValue> { Name = "SimpleChar" };
            SelectRangeChar = new NonTerminal<TValue> { Name = "SelectRangeChar" };
            SelectChar = new NonTerminal<TValue> { Name = "SelectChar" };
            SelectNotChar = new NonTerminal<TValue> { Name = "SelectNotChar" };
            CharList = new NonTerminal<IList<TValue>> { Name = "CharList" };
            SingleChar = new NonTerminal<TValue> { Name = "SingleChar" };
            SubExpr = new NonTerminal<TValue> { Name = "SubExpr" };
            QuantExpr = new NonTerminal<TValue> { Name = "QuantExpr" };
            ConcatExpr = new NonTerminal<TValue> { Name = "ConcatExpr" };
            Expr = new NonTerminal<TValue> { Name = "Expr" };

            // Rules

            var rules = new GrammarRule[] {
                RegexRule = new GrammarRule<TValue, TValue>{LeftHandSide = Expr, RightHandSide = new[] {ConcatExpr}},
                ConcatRule = new GrammarRule<TValue, TValue, TValue>{LeftHandSide = ConcatExpr, RightHandSide = new[] {ConcatExpr, QuantExpr}, Name="RegexConcat"},
                AlternateRule = new GrammarRule<TValue, TValue, TValue> { LeftHandSide = ConcatExpr, RightHandSide = new GrammarSymbol[] { ConcatExpr, pipe, QuantExpr },
                    Name="RegexAlternate"},
                new GrammarRule{LeftHandSide = ConcatExpr, RightHandSide = new[] {QuantExpr}},
                new GrammarRule{LeftHandSide = QuantExpr, RightHandSide = new[] {SubExpr}},
                OneOrMoreRule = new GrammarRule<TValue, TValue>{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, plus}, Name="RegexOneOrMore"},
                ZeroOrOneRule = new GrammarRule<TValue, TValue>{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, question}, Name="RegexZeroOrOne"},
                ZeroOrMoreRule = new GrammarRule<TValue, TValue>{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, star}, Name="RegexZeroOrMore"},
                new GrammarRule{LeftHandSide = SubExpr, RightHandSide = new GrammarSymbol[] {openNonCapturing, ConcatExpr, closeParen}},
                CapturingGroupRule = new GrammarRule<TValue, TValue> { LeftHandSide = SubExpr, RightHandSide = new GrammarSymbol[] { openParen, ConcatExpr, closeParen }, 
                    Name = "RegexCapturingGroup" },
                new GrammarRule{LeftHandSide = SubExpr, RightHandSide = new [] {SingleChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectNotChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectRangeChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SimpleChar}},
                SelectNotCharRule = new GrammarRule<IList<TValue>, TValue>{LeftHandSide = SelectNotChar, RightHandSide = new GrammarSymbol[] {openBracketCaret, CharList, closeBracket}, 
                    Name="RegexSelectNotChar"},
                SelectCharRule = new GrammarRule<IList<TValue>, TValue>{LeftHandSide = SelectChar, RightHandSide = new GrammarSymbol[] {openBracket, CharList, closeBracket},
                    Name="RegexSelectChar"},
                SelectRangeCharRule = new GrammarRule<TValue, TValue, TValue>{LeftHandSide = SelectRangeChar, RightHandSide = new GrammarSymbol[] {SpecificChar, dash, SpecificChar},
                    Name="RegexSelectRangeChar"},
                SelectCharListSingleonRule = new GrammarRule<TValue, IList<TValue>>{LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {SingleChar}, Name="RegexCharListSingleton"},
                SelectCharListAppendRule = new GrammarRule<IList<TValue>,TValue, IList<TValue>>
                    {LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {CharList, SingleChar}, Name="RegexCharListAppend"},
                new GrammarRule{LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {SpecificChar}},
                new GrammarRule{LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {CharClassEscape}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {OtherChar}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {SingleCharEscape}},
            };
            Init(rules);
        }

    }
}
