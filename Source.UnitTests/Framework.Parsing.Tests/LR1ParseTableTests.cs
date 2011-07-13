using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Framework.CodeGen;
using Framework.CodeGen.Expressions;
using Framework.Parsing;
using NUnit.Framework;
using Source.UnitTests.Framework.Parsing.Tests;

namespace ParsingTests
{
    [TestFixture]
    public class LR1ParseTableTests
    {
        Expression<Func<char, bool>> _matchDigit = (digit) => char.IsDigit(digit);
        Expression<Func<char, bool>> _matchPeriod = (period) => period == '.';

        Terminal<char> _add;
        Terminal<char> _sub;
        Terminal<char> _mul;
        Terminal<char> _div;
        Terminal<char> _exponent;
        Terminal<char> _openParen;
        Terminal<char> _closeParen;
        Terminal<char> _whitespace;

        FiniteAutomatonState<char> _numberStart;
        FiniteAutomatonState<char> _variableStart;

        TerminalClassifier<char> _classifierGen;
        IExpressionHelper _expressionHelper;
        Func<string, FiniteAutomatonState<char>> _regexCompiler;

        Terminal<char> GetNumberTerminal<T>(Expression<Func<string,T>> action)
        {

            /*
            var term = new Terminal<char> { Name = "Number", ValueType = typeof(T),
                InitialState = _regexCompiler(@"\d+(\.\d+)?")};
            return term;
             */
            
            Terminal<char> term = new Terminal<char>
            {
                Name = "Number",
                ValueType = typeof(T)
            };

            FiniteAutomatonState<char> digitAfterDecimal = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                Transitions = new [] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = _matchDigit
                    }
                }
            };
            digitAfterDecimal.Transitions.First().Target = digitAfterDecimal;
            
            FiniteAutomatonState<char> firstDigitAfterDecimal = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = _matchDigit,
                        Target = digitAfterDecimal
                    }
                }
            };

            FiniteAutomatonState<char> digitBeforeDecimal = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = _matchDigit
                    },
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = _matchPeriod,
                        Target = firstDigitAfterDecimal
                    }
                }
            };
            digitBeforeDecimal.Transitions.First().Target = digitBeforeDecimal;

            FiniteAutomatonState<char> firstDigitBeforeDecimal = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = _matchDigit,
                        Target = digitBeforeDecimal
                    }
                }
            };

            term.InitialState = firstDigitBeforeDecimal;
            return term;
             
        }

        public Terminal<char> GetLiteralMatcher(string literal)
        {

            Terminal<char> term = new Terminal<char>();
            FiniteAutomatonState<char>[] states = new FiniteAutomatonState<char>[literal.Length + 1];

            states[literal.Length] = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
            int i;
            for (i = literal.Length - 1; i >= 0; --i)
            {
                // TODO: stop creating duplicate expressions that run the same tests.
                char ch = literal[i];
                ParameterExpression px = Expression.Parameter(typeof(char), "litcheck" + ch);

                states[i] = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            CharacterMatchExpression = Expression.Lambda<Func<char,bool>>(Expression.Equal(px, Expression.Constant(ch)), px),
                            Target = states[i+1]
                        }
                    }
                };
            }
            term.InitialState = states[0];
            return term;
        }

        [TestFixtureSetUp]
        public void init()
        {
            _expressionHelper = new ExpressionHelper();

            var builder = new RegexCharNFABuilder(_expressionHelper);
            var expr = builder.CreateRegexParser("TestRegexCompile");
            _regexCompiler = expr.Compile();

            _classifierGen = new TerminalClassifier<char>(_expressionHelper);
            // Terminals with no associated parse value.

            // Whitespace
            _whitespace = new Terminal<char>
            {
                Name = "Whitespace",
                ValueType = typeof(void)
            };

            Expression<Func<char, bool>> matchSpace = (c) => char.IsWhiteSpace(c);
            var whitespaceChar = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchSpace
                    }
                }
            };

            whitespaceChar.Transitions.First().Target = whitespaceChar;

            var firstWhitespace = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchSpace,
                        Target=whitespaceChar
                    }
                }
            };

            _whitespace.InitialState = firstWhitespace;

            // Arithmetic operators
            _add = GetLiteralMatcher("+");
            _add.Name = "Add";
            _sub = GetLiteralMatcher("-");
            _sub.Name = "Sub";
            _mul = GetLiteralMatcher("*");
            _mul.Name = "Mul";
            _div = GetLiteralMatcher("/");
            _div.Name = "Div";
            _exponent = GetLiteralMatcher("^");
            _exponent.Name = "Exponent";
            _openParen = GetLiteralMatcher("(");
            _openParen.Name = "OpenParen";
            _closeParen = GetLiteralMatcher(")");
            _closeParen.Name = "CloseParen";
        }

        enum TokenType
        {
            Number,
            Add,
            Sub,
            Mul,
            Div,
            Exponent,
            OpenParen,
            CloseParen,
            Eof,
            None
        };

        [Test]
        public void TestNumberMatch()
        {
            var term = GetNumberTerminal<double>(x => double.Parse(x));
            Expression<Func<TestStringInput, string, double>> capture = ((t, s) => double.Parse(s));

            var classifier = _classifierGen.Classifier<TestStringInput, double>()
                .HasCurrentCharExprIs(r => r.HasCurrentChar())
                .CurrentCharExprIs(r => r.CurrentChar())
                .MoveNextCharExprIs(r => r.MoveNextChar())
                .MarkPosExprIs(r => r.MarkPos())
                .UnmarkPosExprIs(r => r.UnmarkPos())
                .GetFromMarkExprIs(r => r.GetFromMarkedPos())
                .AddSkipTerminal(_whitespace)
                .RejectHandlerIs(r => 0.0)
                .AddTerminalHandler(term, capture)
                .Generate();

            var f = classifier.Compile();
            Assert.AreEqual(123.0, f(new TestStringInput("123")));
            Assert.AreEqual(3.14, f(new TestStringInput("3.14")));
        }

        [Test]
        public void TestTokenize()
        {
            var term = GetNumberTerminal<double>(x => double.Parse(x));
            var classifier = _classifierGen.Classifier<TestStringInput, TokenType>()
                .HasCurrentCharExprIs(r => r.HasCurrentChar())
                .CurrentCharExprIs(r => r.CurrentChar())
                .MoveNextCharExprIs(r => r.MoveNextChar())
                .MarkPosExprIs(r => r.MarkPos())
                .UnmarkPosExprIs(r => r.UnmarkPos())
                .GetFromMarkExprIs(r => r.GetFromMarkedPos())
                .RejectHandlerIs(r => TokenType.None)
                .AddSkipTerminal(_whitespace)
                .AddTerminalHandler(term, r => TokenType.Number)
                .AddTerminalHandler(_add, r => TokenType.Add)
                .AddTerminalHandler(_sub, r => TokenType.Sub)
                .AddTerminalHandler(_mul, r => TokenType.Mul)
                .AddTerminalHandler(_div, r => TokenType.Div)
                .AddTerminalHandler(_exponent, r => TokenType.Exponent)
                .AddTerminalHandler(_openParen, r => TokenType.OpenParen)
                .AddTerminalHandler(_closeParen, r => TokenType.CloseParen)
                .EofHandlerIs(r => TokenType.Eof)
                .Generate();

            var si = new ParseState<double>("(2+3.5) *((7.321^2)+1)");
            var f = classifier.Compile();
            Assert.AreEqual(TokenType.OpenParen, f(si));
            Assert.AreEqual(TokenType.Number, f(si));
            Assert.AreEqual(TokenType.Add, f(si));
            Assert.AreEqual(TokenType.Number, f(si));
            Assert.AreEqual(TokenType.CloseParen, f(si));
            Assert.AreEqual(TokenType.Mul, f(si));
            Assert.AreEqual(TokenType.OpenParen, f(si));
            Assert.AreEqual(TokenType.OpenParen, f(si));
            Assert.AreEqual(TokenType.Number, f(si));
            Assert.AreEqual(TokenType.Exponent, f(si));
            Assert.AreEqual(TokenType.Number, f(si));
            Assert.AreEqual(TokenType.CloseParen, f(si));
            Assert.AreEqual(TokenType.Add, f(si));
            Assert.AreEqual(TokenType.Number, f(si));
            Assert.AreEqual(TokenType.CloseParen, f(si));
            Assert.AreEqual(TokenType.Eof, f(si));
        }

        public class ParseState<ExpType> : TestStringInput
        {
            public ParseState(string s)
                : base(s)
            {
            }

            int _currentTerminal;

            public int CurrentTerminal 
            {
                get
                {
                    return _currentTerminal;
                }
                set
                {
                    _currentTerminal = value;
                }
            }

            ExpType _currentTerminalValue;
            public ExpType CurrentTerminalValue
            {
                get
                {
                    return _currentTerminalValue;
                }
                set
                {
                    _currentTerminalValue = value;
                }
            }

            int _currentNonTerminal;
            public int CurrentNonTerminal
            {
                get
                {
                    return _currentNonTerminal;
                }

                set
                {
                    _currentNonTerminal = value;
                }
            }

            ExpType _currentNonTerminalValue;
            public ExpType CurrentNonTerminalValue
            {
                get
                {
                    return _currentNonTerminalValue;
                }
                set
                {
                    _currentNonTerminalValue = value;
                }
            }

            public List<ExpType> CurrentNonTerminalListValue
            {
                get;
                set;
            }
        }

        public abstract class RegexParseState : ParseState<string>
        {
            public RegexParseState(string x)
                : base(x)
            {
            }

            public abstract void Concat(string x1, string x2);
        }

        public static List<T> GetAppend<T>(List<T> l, T item)
        {
            l.Add(item);
            return l;
        }

        [Test]
        public void TestRegexParse()
        {
            RegexGrammar g = new RegexGrammar();
            // Value types for symbols.
            g.CharClassEscape.ValueType = typeof(string);
            g.CharList.ValueType = typeof(List<string>);
            g.ConcatExpr.ValueType = typeof(string);
            g.Expr.ValueType = typeof(string);
            g.OtherChar.ValueType = typeof(string);
            g.QuantExpr.ValueType = typeof(string);
            g.SelectChar.ValueType = typeof(string);
            g.SelectNotChar.ValueType = typeof(string);
            g.SelectRangeChar.ValueType = typeof(string);
            g.SimpleChar.ValueType = typeof(String);
            g.SingleChar.ValueType = typeof(String);
            g.SingleCharEscape.ValueType = typeof(string);
            g.SpecificChar.ValueType = typeof(string);
            g.SubExpr.ValueType = typeof(string);

            // Actions for rules
            g.CapturingGroupRule.Action = ((Expression<Func<string,string>>)((s)=>s + "Capture(" + s
                + ")"));
            g.ConcatRule.Action = ((Expression<Func<string,string,string>>)((s1,s2)=>"Concat(" + s1 +
                ", " + s2 + ")"));
            g.AlternateRule.Action = ((Expression<Func<string, string, string>>)((s1, s2) => "Alternate("
                + s1 + ", " + s2 + ")"));
            g.SelectCharListAppendRule.Action = ((Expression<Func<List<string>, string, List<string>>>)
                ((l, s) => GetAppend(l,s)));
            g.SelectCharListSingleonRule.Action = ((Expression<Func<string, List<string>>>)
                ((s) => new List<string> { s }));
            g.SelectCharRule.Action = ((Expression<Func<List<string>, string>>)
                ((l) => (l.Count == 1) ? l[0] : "SelectChar(" + string.Join(", ", l) + ")"));
            g.SelectNotCharRule.Action = ((Expression<Func<List<string>, string>>)
                ((l) => "SelectNotChar(" + string.Join(", ", l) + ")"));
            g.SelectRangeCharRule.Action = ((Expression<Func<string, string, string>>)
                ((s1, s2) => "SelectRange(" + s1 + ", " + s2 + ")"));
            g.OneOrMoreRule.Action = ((Expression<Func<string, string>>)
                ((s) => "OneOrMore(" + s + ")"));
            g.ZeroOrMoreRule.Action = ((Expression<Func<string, string>>)
                ((s) => "ZeroOrMore(" + s + ")"));
            g.ZeroOrOneRule.Action = ((Expression<Func<string, string>>)
                ((s) => "ZeroOrOne(" + s + ")"));
            g.RegexRule.Action = ((Expression<Func<string, string>>)
                ((s) => "Regex(" + s + ")"));
            
            // Time to construct a parser.
            // TODO: Some of this needs to be rolled up even more.
                        var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);

            var classifier = _classifierGen.Classifier<ParseState<object>, object>()
                .HasCurrentCharExprIs(ps => ps.HasCurrentChar())
                .CurrentCharExprIs(ps => ps.CurrentChar())
                .MoveNextCharExprIs(ps => ps.MoveNextChar())
                .MarkPosExprIs(ps => ps.MarkPos())
                .UnmarkPosExprIs(ps => ps.UnmarkPos())
                .GetFromMarkExprIs(ps => ps.GetFromMarkedPos());

            var parserGen = new ParserGenerator<char>(_expressionHelper);

            var parser = parserGen.NewSession<ParseState<object>>()
                .TerminalIs(ps => ps.CurrentTerminal)
                .NonTerminalIs(ps => ps.CurrentNonTerminal)
                .TerminalValueExprIs<string>(ps => (string)ps.CurrentTerminalValue)
                .NonTerminalValueExprIs<string>(ps => (string)ps.CurrentNonTerminalValue)
                .NonTerminalValueExprIs<List<string>>(ps=>(List<string>)ps.CurrentNonTerminalValue)
                //.IncludeSymbols(true)
                .Generate("ParseExpr", parseTable, classifier);

            var f = parser.Compile();

            // Now to run some tests
            var si = new ParseState<object>("xyz");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(Concat(x, y), z))", si.CurrentNonTerminalValue);
            si = new ParseState<object>("[^abc]123");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(Concat(Concat(SelectNotChar(a, b, c), 1), 2), 3))",
                si.CurrentNonTerminalValue);
            si = new ParseState<object>("[123]*4+\\\"");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(Concat(ZeroOrMore(SelectChar(1, 2, 3)), OneOrMore(4)), \\\"))",
                si.CurrentNonTerminalValue);
            //si = new ParseState<string>(@"[^\:]*://(\d+\.\d+\.\d+\.\d+)(?:\:[^/]*)?/([^/?]*).*");
            // TODO: Inside of a [] or [^] construct, we need to take just about any character that's
            // not a ] literally.  Also, we need to support [a-bc-d]
            si = new ParseState<object>(@"[^\:]*://(\d+\.\d+\.\d+\.\d+)(?:\:[^/]*)?/([^/\?]*)");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(Concat(Concat(Concat(Concat(Concat(Concat(ZeroOrMore(SelectNotChar(\\:)), :), /), /), Concat(Concat(Concat(Concat(Concat(Concat(OneOrMore(\\d), \\.), OneOrMore(\\d)), \\.), OneOrMore(\\d)), \\.), OneOrMore(\\d))Capture(Concat(Concat(Concat(Concat(Concat(Concat(OneOrMore(\\d), \\.), OneOrMore(\\d)), \\.), OneOrMore(\\d)), \\.), OneOrMore(\\d)))), ZeroOrOne(Concat(\\:, ZeroOrMore(SelectNotChar(/))))), /), ZeroOrMore(SelectNotChar(/, \\?))Capture(ZeroOrMore(SelectNotChar(/, \\?)))))", si.CurrentNonTerminalValue);

            si = new ParseState<object>("[a-z]|[A-Z](?:[a-z]|[A-Z]|[0-9])*");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(Alternate(SelectRange(a, z), SelectRange(A, Z)), ZeroOrMore(Alternate(Alternate(SelectRange(a, z), SelectRange(A, Z)), SelectRange(0, 9)))))",
                si.CurrentNonTerminalValue);

            si = new ParseState<object>("[a-zA-z](?:[a-zA-z0-9_])*");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual("Regex(Concat(SelectChar(SelectRange(a, z), SelectRange(A, z)), ZeroOrMore(SelectChar(SelectRange(a, z), SelectRange(A, z), SelectRange(0, 9), _))))",
                si.CurrentNonTerminalValue);
        }

        

        [Test]
        public void TestRegexCompile()
        {
            var builder = new RegexCharNFABuilder(_expressionHelper);
            var expr = builder.CreateRegexParser("TestRegexCompile");
            var regexCompiler = expr.Compile();

            var t1 = new Terminal<char>
            {
                Name = "T1",
                InitialState = regexCompiler("x")
            };
            var classifier = _classifierGen.Classifier<TestStringInput, bool>()
                .HasCurrentCharExprIs(r => r.HasCurrentChar())
                .CurrentCharExprIs(r => r.CurrentChar())
                .MoveNextCharExprIs(r => r.MoveNextChar())
                .MarkPosExprIs(r => r.MarkPos())
                .UnmarkPosExprIs(r => r.UnmarkPos())
                .GetFromMarkExprIs(r => r.GetFromMarkedPos())
                .RejectHandlerIs(x => false)
                .EofHandlerIs(x => false);

            var clexpr = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t1, x => true } });
            var f = clexpr.Compile();
            Assert.IsTrue(f(new TestStringInput("x")));
            Assert.IsFalse(f(new TestStringInput("abcsx")));
            Assert.IsFalse(f(new TestStringInput("yyyx")));

            var t2 = new Terminal<char> { Name = "T2", InitialState = regexCompiler("a*b") };
            f = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t2, x => true } })
                .Compile();
            Assert.IsFalse(f(new TestStringInput("x")));
            Assert.IsTrue(f(new TestStringInput("aaaaaab")));
            Assert.IsTrue(f(new TestStringInput("b")));
            Assert.IsTrue(f(new TestStringInput("ab")));
            Assert.IsFalse(f(new TestStringInput("aaaaa")));

            var t3 = new Terminal<char> {Name = "T3", InitialState = regexCompiler(@"\d")};
            f = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t3, x => true } })
                .Compile();
            Assert.IsTrue(f(new TestStringInput("243")));
            Assert.IsTrue(f(new TestStringInput("9")));
            Assert.IsFalse(f(new TestStringInput("ac39")));

            var t4 = new Terminal<char> { Name = "T4", InitialState = regexCompiler(@"\d+\.") };
            f = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t4, x => true } })
                .Compile();
            Assert.IsTrue(f(new TestStringInput("143.")));
            Assert.IsTrue(f(new TestStringInput("4.3")));
            Assert.IsFalse(f(new TestStringInput(".22")));
            Assert.IsFalse(f(new TestStringInput("43")));

            var t5 = new Terminal<char> { Name = "T5", InitialState = regexCompiler(@"\d+(\.\d+)") };
            f = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t5, x=>true}})
                .Compile();
            Assert.IsTrue(f(new TestStringInput("143.5")));
            Assert.IsFalse(f(new TestStringInput("143")));

            // TODO: Find out why the ? quantifier doesn't work.
            //var t6 = new Terminal<char> { Name = "T6", InitialState = regexCompiler(@"\d+(\.\d+)?") };
            //f = classifier.Generate(new Dictionary<Terminal<char>, Expression<Func<TestStringInput, bool>>> { { t6, x => true } })
            //    .Compile();
            //Assert.IsTrue(f(new TestStringInput("143.5")));
            //Assert.IsTrue(f(new TestStringInput("143")));
        }

        [Test]
        public void TestParse()
        {
            var number = GetNumberTerminal<double>(x => double.Parse(x));
            number.Action = ((Expression<Func<string, double>>)((s) => double.Parse(s)));
            var exponentExpression = new NonTerminal { Name = "ExponentExpression", ValueType = typeof(double) };
            var mulExpression = new NonTerminal { Name = "MultiplyExpression", ValueType = typeof(double) };
            var addExpression = new NonTerminal { Name = "AddExpression", ValueType = typeof(double) };
            var expression = new NonTerminal { Name = "Expression", ValueType = typeof(double) };


            var g = new Grammar<char>(new GrammarRule[] {
                new GrammarRule{LeftHandSide = expression, RightHandSide = new GrammarSymbol[] {addExpression}},
                new GrammarRule{LeftHandSide = addExpression, RightHandSide = new GrammarSymbol[] {mulExpression}},
                new GrammarRule{LeftHandSide = addExpression, RightHandSide = new GrammarSymbol[] {addExpression, _add, mulExpression},
                    Action = ((Expression<Func<double,double,double>>)((a,b)=>a+b))},
                new GrammarRule{LeftHandSide = addExpression, RightHandSide = new GrammarSymbol[] {addExpression, _sub, mulExpression},
                    Action = ((Expression<Func<double,double,double>>)((a,b)=>a-b))},
                new GrammarRule{LeftHandSide = mulExpression, RightHandSide = new GrammarSymbol[] {exponentExpression}},
                new GrammarRule{LeftHandSide = mulExpression, RightHandSide = new GrammarSymbol[] {mulExpression, _mul, exponentExpression},
                    Action = ((Expression<Func<double,double,double>>)((a,b)=>a*b))},
                new GrammarRule{LeftHandSide = mulExpression, RightHandSide = new GrammarSymbol[] {mulExpression, _div, exponentExpression},
                    Action = ((Expression<Func<double,double,double>>)((a,b)=>a/b))},
                new GrammarRule{LeftHandSide = exponentExpression, RightHandSide = new GrammarSymbol[] {number}},
                new GrammarRule{LeftHandSide = exponentExpression, RightHandSide = new GrammarSymbol[] {exponentExpression, _exponent, number},
                    Action = ((Expression<Func<double,double,double>>)((a,b)=>Math.Pow(a,b)))},
                new GrammarRule{LeftHandSide = exponentExpression, RightHandSide = new GrammarSymbol[] {_openParen, expression, _closeParen}}
            });

            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);
            parseTable.Dump();

            var classifier = _classifierGen.Classifier<ParseState<double>, double>()
                .HasCurrentCharExprIs(ps => ps.HasCurrentChar())
                .CurrentCharExprIs(ps => ps.CurrentChar())
                .MoveNextCharExprIs(ps => ps.MoveNextChar())
                .MarkPosExprIs(ps => ps.MarkPos())
                .UnmarkPosExprIs(ps => ps.UnmarkPos())
                .GetFromMarkExprIs(ps => ps.GetFromMarkedPos())
                .AddSkipTerminal(_whitespace);

            var parserGen = new ParserGenerator<char>(_expressionHelper);

            var parser = parserGen.NewSession<ParseState<double>>()
                .TerminalIs(ps => ps.CurrentTerminal)
                .NonTerminalIs(ps => ps.CurrentNonTerminal)
                .TerminalValueExprIs<double>(ps => ps.CurrentTerminalValue)
                .NonTerminalValueExprIs<double>(ps => ps.CurrentNonTerminalValue)
                //.IncludeSymbols(true)
                .Generate("ParseExpr", parseTable, classifier);

            var f = parser.Compile();

            // Now we try it out.
            var si = new ParseState<double>("1+4*3^2");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual(37.0, si.CurrentNonTerminalValue);

            si = new ParseState<double>("(1+4)*3^2");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual(45.0, si.CurrentNonTerminalValue);

            si = new ParseState<double>("((1+4)*3)^2");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual(225.0, si.CurrentNonTerminalValue);

            si = new ParseState<double>("3*(4+(5-3))");
            Assert.AreEqual(0, f(si));
            Assert.AreEqual(18.0, si.CurrentNonTerminalValue);
        }
    }

}
