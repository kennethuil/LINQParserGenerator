using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Framework.Parsing
{
    public class RegexGrammar : Grammar<char>
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
        public Terminal<char> SingleCharEscape { get; private set; }

        // An escape sequence that matches any one of several characters (such as "\d" which matches any digit)
        public Terminal<char> CharClassEscape { get; private set; }

        // A single-character expression that is not an escape sequence
        public Terminal<char> OtherChar { get; private set; }

        // Any expression that matches one specific single character
        public NonTerminal SpecificChar { get; private set; }

        // An expression that is a single literal character or a single escape sequence
        public NonTerminal SimpleChar { get; private set; }

        // An expression that matches any one of a range of characters
        public NonTerminal SelectRangeChar { get; private set; }

        // An expression that matches any one of an explicit list of characters
        public NonTerminal SelectChar { get; private set; }

        // An expression that matches any one character that is not in the explicit list
        public NonTerminal SelectNotChar { get; private set; }

        // Represent the explicit list of characters that is part of a selection
        public NonTerminal CharList { get; private set; }

        // Any expression that matches a sequence of exactly one character, but it might match any one of several single-character sequences
        public NonTerminal SingleChar { get; private set; }

        // An expression that either matches a single character or is a grouped (capturing or non-capturing) expression.
        public NonTerminal SubExpr { get; private set; }

        // An expression with a quantifier applied
        public NonTerminal QuantExpr { get; private set; }

        // An expression that concatenates two other expressions
        public NonTerminal ConcatExpr { get; private set; }

        // A regular expression
        public NonTerminal Expr { get; private set; }

        public GrammarRule RegexRule { get; private set; }
        public GrammarRule ConcatRule { get; private set; }
        public GrammarRule AlternateRule { get; private set; }
        public GrammarRule ZeroOrMoreRule { get; private set; }
        public GrammarRule OneOrMoreRule { get; private set; }
        public GrammarRule ZeroOrOneRule { get; private set; }
        public GrammarRule CapturingGroupRule { get; private set; }
        public GrammarRule SelectCharRule { get; private set; }
        public GrammarRule SelectNotCharRule { get; private set; }
        public GrammarRule SelectRangeCharRule { get; private set; }
        public GrammarRule SelectCharListAppendRule { get; private set; }
        public GrammarRule SelectCharListSingleonRule { get; private set; }


        static Terminal<char> MatchCharSequence(string terminalName, 
            params Expression<Func<char, bool>>[] charMatches)
        {
            // Terminals
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
            var term = new Terminal<char>
            {
                InitialState = states[0],
                Name = terminalName
            };
            return term;
        }

        // TODO: Inside of a [] or [^] construct, we need to take just about any character that's
        // not a ] literally.  Also, we need to support [a-bghyc-d]

        public RegexGrammar()
        {
            SingleCharEscape = MatchCharSequence("SingleCharEscape", _matchBackslash, _matchSingleCharEscapeChar);
            CharClassEscape = MatchCharSequence("CharClassEscape", _matchBackslash, _matchCharClassEscapeChar);
            // TODO: Hex & Unicode escape
            OtherChar = MatchCharSequence("OtherChar", _matchNonControlChar);

            var openNonCapturing = MatchCharSequence("OpenNonCapturing", _matchOpenParen, _matchQuestion, _matchColon);
            var openParen = MatchCharSequence("OpenParen", _matchOpenParen);
            var closeParen = MatchCharSequence("CloseParen", _matchCloseParen);
            var openBracket = MatchCharSequence("OpenBracket", _matchOpenBracket);
            var openBracketCaret = MatchCharSequence("OpenBracketCaret", _matchOpenBracket,
                _matchCaret);
            var closeBracket = MatchCharSequence("CloseBracket", _matchCloseBracket);
            
            var star = MatchCharSequence("Star", _matchStar);
            var plus = MatchCharSequence("Plus", _matchPlus);
            var question = MatchCharSequence("Question", _matchQuestion);
            var dash = MatchCharSequence("Dash", _matchDash);
            var pipe = MatchCharSequence("Pipe", _matchPipe);

            // NonTerminals.
            SpecificChar = new NonTerminal { Name = "SpecificChar" };
            SimpleChar = new NonTerminal { Name = "SimpleChar" };
            SelectRangeChar = new NonTerminal { Name = "SelectRangeChar" };
            SelectChar = new NonTerminal { Name = "SelectChar" };
            SelectNotChar = new NonTerminal { Name = "SelectNotChar" };
            CharList = new NonTerminal { Name = "CharList" };
            SingleChar = new NonTerminal { Name = "SingleChar" };
            SubExpr = new NonTerminal { Name = "SubExpr" };
            QuantExpr = new NonTerminal { Name = "QuantExpr" };
            ConcatExpr = new NonTerminal { Name = "ConcatExpr" };
            Expr = new NonTerminal { Name = "Expr" };

            // Rules

            var rules = new GrammarRule[] {
                RegexRule = new GrammarRule{LeftHandSide = Expr, RightHandSide = new[] {ConcatExpr}},
                ConcatRule = new GrammarRule{LeftHandSide = ConcatExpr, RightHandSide = new[] {ConcatExpr, QuantExpr}, Name="RegexConcat"},
                AlternateRule = new GrammarRule { LeftHandSide = ConcatExpr, RightHandSide = new GrammarSymbol[] { ConcatExpr, pipe, QuantExpr },
                    Name="RegexAlternate"},
                new GrammarRule{LeftHandSide = ConcatExpr, RightHandSide = new[] {QuantExpr}},
                new GrammarRule{LeftHandSide = QuantExpr, RightHandSide = new[] {SubExpr}},
                OneOrMoreRule = new GrammarRule{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, plus}, Name="RegexOneOrMore"},
                ZeroOrOneRule = new GrammarRule{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, question}, Name="RegexZeroOrOne"},
                ZeroOrMoreRule = new GrammarRule{LeftHandSide = QuantExpr, RightHandSide = new GrammarSymbol[] {SubExpr, star}, Name="RegexZeroOrMore"},
                new GrammarRule{LeftHandSide = SubExpr, RightHandSide = new GrammarSymbol[] {openNonCapturing, ConcatExpr, closeParen}},
                CapturingGroupRule = new GrammarRule { LeftHandSide = SubExpr, RightHandSide = new GrammarSymbol[] { openParen, ConcatExpr, closeParen }, 
                    Name = "RegexCapturingGroup" },
                new GrammarRule{LeftHandSide = SubExpr, RightHandSide = new [] {SingleChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectNotChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SelectRangeChar}},
                new GrammarRule{LeftHandSide = SingleChar, RightHandSide = new [] {SimpleChar}},
                SelectNotCharRule = new GrammarRule{LeftHandSide = SelectNotChar, RightHandSide = new GrammarSymbol[] {openBracketCaret, CharList, closeBracket}, 
                    Name="RegexSelectNotChar"},
                SelectCharRule = new GrammarRule{LeftHandSide = SelectChar, RightHandSide = new GrammarSymbol[] {openBracket, CharList, closeBracket},
                    Name="RegexSelectChar"},
                SelectRangeCharRule = new GrammarRule{LeftHandSide = SelectRangeChar, RightHandSide = new GrammarSymbol[] {SpecificChar, dash, SpecificChar},
                    Name="RegexSelectRangeChar"},
                SelectCharListSingleonRule = new GrammarRule{LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {SingleChar}, Name="RegexCharListSingleton"},
                SelectCharListAppendRule = new GrammarRule{LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {CharList, SingleChar}, Name="RegexCharListAppend"},
                new GrammarRule{LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {SpecificChar}},
                new GrammarRule{LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {CharClassEscape}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {OtherChar}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {SingleCharEscape}},
            };
            Init(rules);
        }

    }
}
