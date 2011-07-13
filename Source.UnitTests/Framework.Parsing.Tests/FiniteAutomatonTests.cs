using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Framework.CodeGen;
using Framework.CodeGen.Expressions;
using Framework.Parsing;
using NUnit.Framework;

namespace Source.UnitTests.Framework.Parsing.Tests
{
    public enum TerminalMatch
    {
        None,
        OpenTag,
        CloseTag,
        LeafTag,
        If,
        While,
        Identifier,
        Eof
    };

    public class TestStringInput
    {
        String _input;
        int _pos;
        int? _markPos;
        string _lastCapture;

        public TestStringInput(string input)
        {
            _input = input;
        }

        public bool HasCurrentChar()
        {
            return _pos < _input.Length;
        }

        public char CurrentChar()
        {
            return _input[_pos];
        }

        public void MoveNextChar()
        {
            _pos++;
        }

        public void MarkPos()
        {
            _markPos = _pos;
        }

        public void UnmarkPos()
        {
            _markPos = null;
        }

        public string GetFromMarkedPos()
        {
            var text = _input.Substring(_markPos.Value, _pos - _markPos.Value);
            return text;
        }

        public TerminalMatch Capture(string str, TerminalMatch tt)
        {
            _lastCapture = str;
            return tt;
        }

        public String LastCapture
        {
            get
            {
                return _lastCapture;
            }
        }
    }



    [TestFixture]
    public class FiniteAutomatonTests
    {
        Terminal<char> _openTag;
        Terminal<char> _closeTag;
        Terminal<char> _leafTag;
        Terminal<char> _if;
        Terminal<char> _while;
        Terminal<char> _identifier;
        Terminal<char> _whitespace;
        IExpressionHelper _expressionHelper;
        TerminalClassifier<char> _parserGenerator;

        [TestFixtureSetUp]
        public void Initialize()
        {
            _expressionHelper = new ExpressionHelper();
            Expression<Func<char, bool>> matchOpen = (c) => c == '<';
            Expression<Func<char, bool>> matchClose = (c) => c == '>';
            Expression<Func<char, bool>> matchSlash = (c) => c == '/';
            Expression<Func<char, bool>> matchLetter = (c) => char.IsLetter(c);
            Expression<Func<char, bool>> matchLetterOrDigit = (c) => char.IsLetterOrDigit(c);
            Expression<Func<char, bool>> matchw = (c) => c == 'w';
            Expression<Func<char, bool>> matchh = (c) => c == 'h';
            Expression<Func<char, bool>> matchi = (c) => c == 'i';
            Expression<Func<char, bool>> matchl = (c) => c == 'l';
            Expression<Func<char, bool>> matche = (c) => c == 'e';
            Expression<Func<char, bool>> matchf = (c) => c == 'f';
            Expression<Func<char, bool>> matchSpace = (c) => char.IsWhiteSpace(c);

            _parserGenerator = new TerminalClassifier<char>(_expressionHelper);

            // NOTE: The generator doesn't automatically follow multiple implications yet.
            _parserGenerator.AddImplies(matchLetter, matchLetterOrDigit);
            _parserGenerator.AddImplies(matchw, matchLetter);
            _parserGenerator.AddImplies(matchh, matchLetter);
            _parserGenerator.AddImplies(matchi, matchLetter);
            _parserGenerator.AddImplies(matchl, matchLetter);
            _parserGenerator.AddImplies(matche, matchLetter);
            _parserGenerator.AddImplies(matchf, matchLetter);
            _parserGenerator.AddImplies(matchw, matchLetterOrDigit);
            _parserGenerator.AddImplies(matchh, matchLetterOrDigit);
            _parserGenerator.AddImplies(matchi, matchLetterOrDigit);
            _parserGenerator.AddImplies(matchl, matchLetterOrDigit);
            _parserGenerator.AddImplies(matche, matchLetterOrDigit);
            _parserGenerator.AddImplies(matchf, matchLetterOrDigit);

            // Simple XML tags (no attributes)
            _openTag = new Terminal<char>
            {
                Name = "OpenTag",
                ValueType = typeof(string)
            };
            _closeTag = new Terminal<char>
            {
                Name = "CloseTag",
                ValueType = typeof(string)
            };
            _leafTag = new Terminal<char>
            {
                Name = "LeafTag",
                ValueType = typeof(string)
            };

            // Other terminals commonly found in programming languages.
            _if = new Terminal<char>
            {
                Name = "If",
                ValueType = typeof(void)
            };

            _while = new Terminal<char>
            {
                Name = "While",
                ValueType = typeof(void)
            };

            _identifier = new Terminal<char>
            {
                Name = "Identifier",
                ValueType = typeof(string)
            };

            _whitespace = new Terminal<char>
            {
                Name = "Whitespace",
                ValueType = typeof(void)
            };


            var openTagAccept = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                IsRejecting = false,
            };
            var closeTagAccept = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                IsRejecting = false
            };
            var leafTagAccept = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                IsRejecting = false
            };

            var leafClose = new FiniteAutomatonState<char>
            {
                Transitions = new[] {new FiniteAutomatonStateTransition<char> {
                    CharacterMatchExpression = matchClose,
                    Target = leafTagAccept
                }
                }
            };

            var leafName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchSlash,
                        Target = leafClose},
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetterOrDigit}}
            };
            leafName.Transitions.Skip(1).First().Target = leafName;

            var beginLeafName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetter,
                        Target = leafName}}
            };

            var leafOpen = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchOpen,
                        Target = beginLeafName}
                }
            };

            var closeClose = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchClose,
                        Target = closeTagAccept}
                }
            };

            var closeName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char>
                    {
                        MatchEpsilon = true,
                        Target = closeClose
                    },
                    new FiniteAutomatonStateTransition<char>
                    {
                        CharacterMatchExpression = matchLetterOrDigit
                    }
                }
            };
            closeName.Transitions.Skip(1).First().Target = closeName;

            var beginCloseName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char>
                    {
                        CharacterMatchExpression = matchLetter,
                        Target = closeName
                    }
                }
            };
            var closeSlash = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchSlash,
                        Target = beginCloseName}
                }
            };
            var closeOpen = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchOpen,
                        Target = closeSlash}}
            };

            var openName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchClose,
                        Target = openTagAccept},
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetterOrDigit}
                }
            };
            openName.Transitions.Skip(1).First().Target = openName;

            var beginOpenName = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetter,
                        Target = openName
                    }
                }
            };

            var openOpen = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchOpen,
                        Target = beginOpenName}}
            };

            _openTag.InitialState = openOpen;
            _closeTag.InitialState = closeOpen;
            _leafTag.InitialState = leafOpen;

            // Keywords & identifiers
            var acceptIf = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
            var fIf = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchf,
                        Target = acceptIf
                    }
                }
            };
            var iIf = new FiniteAutomatonState<char>
            {

                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchi,
                        Target = fIf
                    }
                }
            };
            _if.InitialState = iIf;

            var acceptWhile = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };

            var eWhile = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matche,
                        Target = acceptWhile
                    }
                }
            };

            var lWhile = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchl,
                        Target = eWhile}
                }
            };

            var iWhile = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchi,
                        Target = lWhile}
                }
            };

            var hWhile = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchh,
                        Target = iWhile}
                }
            };

            var wWhile = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchw,
                        Target = hWhile}
                }
            };
            _while.InitialState = wWhile;

            var identifierChar = new FiniteAutomatonState<char>
            {
                IsAccepting = true,
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetterOrDigit
                    }
                }
            };
            identifierChar.Transitions.First().Target = identifierChar;

            var identifierBegin = new FiniteAutomatonState<char>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<char> {
                        CharacterMatchExpression = matchLetter,
                        Target = identifierChar
                    }
                }
            };
            _identifier.InitialState = identifierBegin;

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

        }

        [Test]
        public void TestCombineRecognizers()
        {
            Debug.WriteLine("Open tag recognizer:");
            DumpFiniteAutomaton(_openTag.InitialState);
            Debug.WriteLine("---------------------------------------------");
            Debug.WriteLine("");
            Debug.WriteLine("Close tag recognizer:");
            DumpFiniteAutomaton(_closeTag.InitialState);
            Debug.WriteLine("---------------------------------------------");
            Debug.WriteLine("");
            Debug.WriteLine("Leaf tag recognizer");
            DumpFiniteAutomaton(_leafTag.InitialState);
            Debug.WriteLine("---------------------------------------------");
            Debug.WriteLine("");


            var combined = _parserGenerator.CombineRecognizers(new[] { _closeTag, _openTag, _leafTag });

            Debug.WriteLine("Combined recognizer:");
            DumpFiniteAutomaton(combined);
        }

        public static bool Read(TextReader r)
        {
            return r.Read() != -1;
        }

        public static char Peek(TextReader r)
        {
            char ch = (char)r.Peek();
            return ch;
        }

        public static bool HasNext(TextReader r)
        {
            return r.Peek() != -1;
        }


        [Test]
        public void TestCreateClassifier()
        {
            Expression<Func<TextReader, TerminalMatch>> foundOpenTag = r => TerminalMatch.OpenTag;
            Expression<Func<TextReader, TerminalMatch>> foundCloseTag = r => TerminalMatch.CloseTag;
            Expression<Func<TextReader, TerminalMatch>> foundLeafTag = r => TerminalMatch.LeafTag;
            Expression<Func<TextReader, TerminalMatch>> foundNoTag = r => TerminalMatch.None;

            var classifier = _parserGenerator.Classifier<TextReader, TerminalMatch>()
                .HasCurrentCharExprIs(r => r.Peek() != -1)
                .CurrentCharExprIs(r => (char)r.Peek())
                .MoveNextCharExprIs(r => r.Read())
                .AddTerminalHandler(_openTag, r => TerminalMatch.OpenTag)
                .AddTerminalHandler(_closeTag, r => TerminalMatch.CloseTag)
                .AddTerminalHandler(_leafTag, r => TerminalMatch.LeafTag)
                .RejectHandlerIs(r => TerminalMatch.None)
                .Generate();

            var open1 = new StringReader("<Open1>");
            var open2 = new StringReader("<x>");
            var close1 = new StringReader("</Close1>");
            var close2 = new StringReader("</x>");
            var leaf1 = new StringReader("<Leaf/>");
            var leaf2 = new StringReader("<x/>");
            var reject1 = new StringReader("<>");
            var reject2 = new StringReader("</>");
            var reject3 = new StringReader("x/>");
            var reject4 = new StringReader("<reject4");
            var reject5 = new StringReader("<reject5/");
            var reject6 = new StringReader("</reject6");
            var reject7 = new StringReader("");

            var f = classifier.Compile();
            Assert.AreEqual(TerminalMatch.OpenTag, f(open1));
            Assert.AreEqual(TerminalMatch.OpenTag, f(open2));
            Assert.AreEqual(TerminalMatch.CloseTag, f(close1));
            Assert.AreEqual(TerminalMatch.CloseTag, f(close2));
            Assert.AreEqual(TerminalMatch.LeafTag, f(leaf1));
            Assert.AreEqual(TerminalMatch.LeafTag, f(leaf2));
            Assert.AreEqual(TerminalMatch.None, f(reject1));
            Assert.AreEqual(TerminalMatch.None, f(reject2));
            Assert.AreEqual(TerminalMatch.None, f(reject3));
            Assert.AreEqual(TerminalMatch.None, f(reject4));
            Assert.AreEqual(TerminalMatch.None, f(reject5));
            Assert.AreEqual(TerminalMatch.None, f(reject6));
            Assert.AreEqual(TerminalMatch.None, f(reject7));
        }

        [Test]
        public void TestClassifyKeywordsAndIdentifiers()
        {
            Expression<Func<TextReader, TerminalMatch>> foundIdentifer = (r) => TerminalMatch.Identifier;
            Expression<Func<TextReader, TerminalMatch>> foundIf = (r) => TerminalMatch.If;
            Expression<Func<TextReader, TerminalMatch>> foundWhile = (r) => TerminalMatch.While;
            Expression<Func<TextReader, TerminalMatch>> foundNone = (r) => TerminalMatch.None;


            var classifier = _parserGenerator.Classifier<TextReader, TerminalMatch>()
                .HasCurrentCharExprIs(r => r.Peek() != -1)
                .CurrentCharExprIs(r => (char)r.Peek())
                .MoveNextCharExprIs(r => r.Read())
                .AddTerminalHandler(_if, r => TerminalMatch.If)
                .AddTerminalHandler(_while, r => TerminalMatch.While)
                .AddTerminalHandler(_identifier, r => TerminalMatch.Identifier)
                .RejectHandlerIs(r => TerminalMatch.None)
                .Generate();



            var f = classifier.Compile();

            var strIf = new StringReader("if");
            var strWhile = new StringReader("while");
            var id1 = new StringReader("bob");
            var id2 = new StringReader("ida");
            var id3 = new StringReader("where");
            var id4 = new StringReader("iffy");
            var id5 = new StringReader("wh");
            var id6 = new StringReader("wherefore");
            var id7 = new StringReader("i");
            var id8 = new StringReader("if8");
            var id9 = new StringReader("d7 = 123");
            var reject1 = new StringReader("99");
            var reject2 = new StringReader("(@*#");
            var reject3 = new StringReader("");

            Assert.AreEqual(TerminalMatch.If, f(strIf));
            Assert.AreEqual(TerminalMatch.While, f(strWhile));
            Assert.AreEqual(TerminalMatch.Identifier, f(id1));
            Assert.AreEqual(TerminalMatch.Identifier, f(id2));
            Assert.AreEqual(TerminalMatch.Identifier, f(id3));
            Assert.AreEqual(TerminalMatch.Identifier, f(id4));
            Assert.AreEqual(TerminalMatch.Identifier, f(id5));
            Assert.AreEqual(TerminalMatch.Identifier, f(id6));
            Assert.AreEqual(TerminalMatch.Identifier, f(id7));
            Assert.AreEqual(TerminalMatch.Identifier, f(id8));
            Assert.AreEqual(TerminalMatch.Identifier, f(id9));
            Assert.AreEqual(TerminalMatch.None, f(reject1));
            Assert.AreEqual(TerminalMatch.None, f(reject2));
            Assert.AreEqual(TerminalMatch.None, f(reject3));
        }


        [Test]
        public void TestClassifyTokensAndCaptureIdentifiers()
        {
            Expression<Func<TestStringInput, string, TerminalMatch>> capture = ((t, m) => t.Capture(m, TerminalMatch.Identifier));

            var classifier = _parserGenerator.Classifier<TestStringInput, TerminalMatch>()
                .HasCurrentCharExprIs(r => r.HasCurrentChar())
                .CurrentCharExprIs(r => r.CurrentChar())
                .MoveNextCharExprIs(r => r.MoveNextChar())
                .MarkPosExprIs(r => r.MarkPos())
                .UnmarkPosExprIs(r => r.UnmarkPos())
                .GetFromMarkExprIs(r => r.GetFromMarkedPos())
                .AddTerminalHandler(_if, r => TerminalMatch.If)
                .AddTerminalHandler(_while, r => TerminalMatch.While)
                .AddTerminalHandler(_identifier, capture)
                .AddSkipTerminal(_whitespace)
                .RejectHandlerIs(r => TerminalMatch.None)
                .Generate();

            var f = classifier.Compile();

            var strIds = new TestStringInput("if while bob ida\nwhere iffy wh wherefore i if8 d7 = 123");

            var reject1 = new TestStringInput("99");
            var reject2 = new TestStringInput("(@*#");
            var reject3 = new TestStringInput("");

            Assert.AreEqual(TerminalMatch.If, f(strIds));
            Assert.AreEqual(TerminalMatch.While, f(strIds));
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("bob", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("ida", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("where", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("iffy", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("wh", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("wherefore", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("i", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("if8", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("d7", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.None, f(reject1));
            Assert.AreEqual(TerminalMatch.None, f(reject2));
            Assert.AreEqual(TerminalMatch.None, f(reject3));
        }

        [Test]
        public void TestClassifyTokensAndCaptureIdentifiersUntilEof()
        {
            Expression<Func<TestStringInput, string, TerminalMatch>> capture = ((t, m) => t.Capture(m, TerminalMatch.Identifier));

            var classifier = _parserGenerator.Classifier<TestStringInput, TerminalMatch>()
                .HasCurrentCharExprIs(r => r.HasCurrentChar())
                .CurrentCharExprIs(r => r.CurrentChar())
                .MoveNextCharExprIs(r => r.MoveNextChar())
                .MarkPosExprIs(r => r.MarkPos())
                .UnmarkPosExprIs(r => r.UnmarkPos())
                .GetFromMarkExprIs(r => r.GetFromMarkedPos())
                .AddTerminalHandler(_if, r => TerminalMatch.If)
                .AddTerminalHandler(_while, r => TerminalMatch.While)
                .AddTerminalHandler(_identifier, capture)
                .AddSkipTerminal(_whitespace)
                .EofHandlerIs(r => TerminalMatch.Eof)
                .RejectHandlerIs(r => TerminalMatch.None)
                .Generate();

            var f = classifier.Compile();

            var strIds = new TestStringInput("if while bob ida\nwhere iffy wh wherefore i if8 d7");

            Assert.AreEqual(TerminalMatch.If, f(strIds));
            Assert.AreEqual(TerminalMatch.While, f(strIds));
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("bob", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("ida", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("where", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("iffy", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("wh", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("wherefore", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("i", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("if8", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Identifier, f(strIds));
            Assert.AreEqual("d7", strIds.LastCapture);
            Assert.AreEqual(TerminalMatch.Eof, f(strIds));
        }



        void DumpFiniteAutomaton(FiniteAutomatonState<char> initial)
        {
            DumpFiniteAutomaton(initial, new HashSet<FiniteAutomatonState<char>>());
        }

        void DumpFiniteAutomaton(FiniteAutomatonState<char> state, ISet<FiniteAutomatonState<char>> visited)
        {
            if (visited.Contains(state))
                return;
            visited.Add(state);

            Debug.WriteLine(state.GetHashCode() + ": ");
            if (state.AcceptTerminals != null)
            {
                foreach (var term in state.AcceptTerminals)
                {
                    Debug.WriteLine("  Accept " + term.Name);
                }
            }
            if (state.RejectTerminals != null)
            {
                foreach (var term in state.RejectTerminals)
                {
                    Debug.WriteLine("  Reject " + term.Name);
                }
            }
            if (state.Transitions != null)
            {
                foreach (var trans in state.Transitions)
                {
                    Debug.WriteLine("  Transition:");
                    Debug.WriteLine("    Match Expression: " + trans.CharacterMatchExpression);
                    Debug.WriteLine("    MatchEof: " + trans.MatchEpsilon);
                    Debug.WriteLine("    MatchEpsilon: " + trans.MatchEpsilon);
                    Debug.WriteLine("    Target: " + trans.Target.GetHashCode());
                }
                foreach (var trans in state.Transitions)
                {
                    DumpFiniteAutomaton(trans.Target, visited);
                }
            }
        }
    }
}
