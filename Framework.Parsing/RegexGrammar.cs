using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Framework.Parsing
{
    public class RegexGrammar<TValue> : Grammar<char>
    {
        // Grammar elements that callers can manipulate.
        public Terminal<char, TValue> WordCharEscape { get; private set; }
        public Terminal<char, TValue> NonWordCharEscape { get; private set; }
        public Terminal<char, TValue> WhitespaceEscape { get; private set; }
        public Terminal<char, TValue> NonWhitespaceEscape { get; private set; }
        public Terminal<char, TValue> DigitEscape { get; private set; }
        public Terminal<char, TValue> NonDigitEscape { get; private set; }

        // An expression that represents a specific single character.
        public NonTerminal<char> SpecificChar { get; private set; }

        // Any expression that matches one specific single character
        public NonTerminal<TValue> SpecificCharMatch { get; private set; }

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
        public GrammarRule<char, char, TValue> SelectRangeCharRule { get; private set; }
        public GrammarRule<IList<TValue>, TValue, IList<TValue>> SelectCharListAppendRule { get; private set; }
        public GrammarRule<TValue, IList<TValue>> SelectCharListSingletonRule { get; private set; }
        public GrammarRule<char, TValue> SpecificCharMatchRule { get; private set; }

        private static readonly ISet<char> _dontMatchNonControlChar =
            new HashSet<char> { '\"', '\\', '[', ']', '(', ')', '?', '+', '*', '-', '^', '|', '-' };
        private static readonly ISet<char> _dontMatchLiteralEscapedChar =
            new HashSet<char> { 'w', 's', 'S', 'd', 'D', 'a', 'b', 't', 'r', 'v', 'f', 'n', 'e' };

        static Terminal<char> MatchLiteral(string terminalName, string str)
        {
            return new Terminal<char>
            {
                // TODO: method overloading and generic type inference not playing well together here.
                InitialState = TerminalClassifier.GetLiteralMatcher<char>(str),
                Name = terminalName
            };
        }

        static Terminal<char, TValue> MatchLiteralValue(string terminalName, string str)
        {
            return new Terminal<char, TValue>
            {
                // TODO: method overloading and generic type inference not playing well together here.
                InitialState = TerminalClassifier.GetLiteralMatcher<char>(str),
                Name = terminalName
            };
        }

        static Terminal<char, char> MatchSingleCharEscape(string terminalName, string escapeSequence, char represented)
        {
            var cRepresented = Expression.Lambda<Func<char>>(Expression.Constant(represented));
            return new Terminal<char, char>
            {
                // TODO: method overloading and generic type inference not playing well together here.
                InitialState = TerminalClassifier.GetLiteralMatcher<char>(escapeSequence),
                Name = terminalName,
                NonCapturingAction = cRepresented
            };
        }


        static Terminal<char, TValue> MatchCapturingCharSequence(string terminalName,
            params ISet<char>[] charMatches)
        {
            //FiniteAutomatonState<char>[] states = GetCharSequenceStates(charMatches);
            var term = new Terminal<char, TValue>
            {
                InitialState = TerminalClassifier.GetSequenceMatcher(charMatches),
                Name = terminalName
            };
            return term;
        }

        public RegexGrammar()
        {
            // Terminals that produce single characters.  They'll get fed into rules that produce TValue.

            // Single character escapes
            var bell = MatchSingleCharEscape("Bell", @"\a", '\a');
            var backspace = MatchSingleCharEscape("Backspace", @"\b", '\b');
            var tab = MatchSingleCharEscape("Tab", @"\t", '\t');
            var cr = MatchSingleCharEscape("CarriageReturn", @"\r", '\r');
            var verticalTab = MatchSingleCharEscape("VerticalTab", @"\v", '\v');
            var formFeed = MatchSingleCharEscape("FormFeed", @"\f", '\f');
            var newline = MatchSingleCharEscape("Newline", @"\n", '\n');
            var escape = MatchSingleCharEscape("Escape", @"\e", '\u001b');

            // TODO: character codes, unicode, etc.

            // Character class escapes
            WordCharEscape = MatchLiteralValue("WordChar", @"\w");
            NonWordCharEscape = MatchLiteralValue("NonWordChar", @"\W");
            WhitespaceEscape = MatchLiteralValue("Whitespace", @"\s");
            NonWhitespaceEscape = MatchLiteralValue("NonWhitespace", @"\S");
            DigitEscape = MatchLiteralValue("Digit", @"\d");
            NonDigitEscape = MatchLiteralValue("NonDigit", @"\D");

            // Literal escape
            var accept = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
            var escapeChar = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        Characters = _dontMatchLiteralEscapedChar,
                        MatchAllExcept = true,
                        Target = accept}
                }
            };
            var backslash = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        Characters = new HashSet<char> {'\\'},
                        Target = escapeChar}
                }
            };
            var literalEscape = new Terminal<char, char>
            {
                InitialState = backslash,
                Name = "LiteralEscape",
                StringAction = (s) => s[1]
            };

            accept = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
            var otherCharStart = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        Characters = _dontMatchNonControlChar,
                        MatchAllExcept = true,
                        Target = accept
                    }
                }
            };
            var otherChar = new Terminal<char, char>
            {
                InitialState = otherCharStart,
                Name = "OtherChar",
                StringAction = (s) => s[0]
            };


            // Punctuation
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

            // NonTerminals
            SpecificCharMatch = new NonTerminal<TValue> { Name = "SpecificCharMatch" };
            SpecificChar = new NonTerminal<char> { Name = "SpecificChar" };
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
                SelectRangeCharRule = new GrammarRule<char,char,TValue>{LeftHandSide = SelectRangeChar, RightHandSide = new GrammarSymbol[] {SpecificChar, dash, SpecificChar},
                    Name = "RegexSelectRangeChar"},
                SelectCharListSingletonRule = new GrammarRule<TValue,IList<TValue>>{LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {SingleChar},
                    Name = "RegexCharListSingleton"},
                SelectCharListAppendRule = new GrammarRule<IList<TValue>,TValue, IList<TValue>>
                    {LeftHandSide = CharList, RightHandSide = new GrammarSymbol[] {CharList, SingleChar}, Name="RegexCharListAppend"},
                
                SpecificCharMatchRule = new GrammarRule<char,TValue> {LeftHandSide = SpecificCharMatch, RightHandSide = new GrammarSymbol[] {SpecificChar},
                    Name = "SpecificCharMatch"},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {bell}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {backspace}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {tab}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {cr}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {verticalTab}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {formFeed}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {newline}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {escape}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {literalEscape}},
                new GrammarRule{LeftHandSide = SpecificChar, RightHandSide = new GrammarSymbol[] {otherChar}},

                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {SpecificCharMatch}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {WordCharEscape}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {NonWordCharEscape}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {WhitespaceEscape}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {NonWhitespaceEscape}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {DigitEscape}},
                new GrammarRule {LeftHandSide = SimpleChar, RightHandSide = new GrammarSymbol[] {NonDigitEscape }}
            };
            Init(rules);
        }
    }
}
