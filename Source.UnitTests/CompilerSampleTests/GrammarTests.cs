﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CompilerSample;
using Framework.CodeGen.Expressions;
using Framework.Parsing;
using NUnit.Framework;

namespace Source.UnitTests.CompilerSampleTests
{
    [TestFixture]
    public class GrammarTests
    {
        public class ParserState : StringInput
        {
            public ParserState(string input) : base(input)
            {
            }

            public int Terminal { get; set; }
            public int NonTerminal { get; set; }
            public string TerminalStringValue { get; set; }
            public int TerminalIntValue { get; set; }
            public double TerminalDoubleValue { get; set; }
            public object NonTerminalValue { get; set; }
            public Dictionary<String, ParameterExpression> Variables { get; set; }
            public readonly ICollection<ParseError<char>> Errors = new List<ParseError<char>>();

            public void SetParameters(IEnumerable<ParameterExpression> p)
            {
                Variables = new Dictionary<string, ParameterExpression>();
                foreach (var pe in p)
                {
                    Variables[pe.Name] = pe;
                }
            }

            public void SetParameters(params ParameterExpression[] p)
            {
                SetParameters((IEnumerable<ParameterExpression>)p);
            }

            public void DeclareVariable(ParameterExpression pe)
            {
                Variables[pe.Name] = pe;
            }

            public Expression GetVariable(string name)
            {
                return Variables[name];
            }
        }

        [Test]
        public void TestParserSucceedsWithoutAValue()
        {
            LanguageGrammar<Expression> g = new LanguageGrammar<Expression>();

            // Spin up a parser for our language.
            // TODO: Package this up and simplify it.
            var expressionHelper = new ExpressionHelper();
            //var classifierGen = new TerminalClassifier<char>();
            var classifierSession = new TerminalClassifierSession<char, ParserState, int>()
                .CurrentCharExprIs(x => x.CurrentChar())
                .GetFromMarkExprIs(x => x.GetFromMarkedPos())
                .HasCurrentCharExprIs(x => x.HasCurrentChar())
                .MarkPosExprIs(x => x.MarkPos())
                .MoveNextCharExprIs(x => x.MoveNextChar())
                .UnmarkPosExprIs(x => x.UnmarkPos());

            var parserGen = new ParserGenerator<char>(expressionHelper);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);
            var session = parserGen.NewSession<ParserState>()
                .NonTerminalValueExprIs<object>(x => x.NonTerminalValue)
                .TerminalValueExprIs<string>(x => x.TerminalStringValue)
                .TerminalValueExprIs<int>(x => x.TerminalIntValue)
                .TerminalValueExprIs<double>(x => x.TerminalDoubleValue)
                .TerminalIs(x => x.Terminal)
                .NonTerminalIs(x => x.NonTerminal)
                .IncludeSymbols(true)
                .UseDefaultValue(true)
                .DebugOutputIs(x => Debug.WriteLine(x))
                .Generate("LanguageParser", parseTable, classifierSession);
            // At this point, session is an Expression<ParserState,int> representing our parser.
            // We can compile it into a delegate or a MethodBuilder.  For the examples, we'll use a delegate.
            var compiler = session.Compile();

            // Create a parser state object and initialize it.
            ParserState ps = new ParserState("x*y+2.0");
            Assert.AreEqual(0, compiler(ps));
        }

        [Test]
        public void TestQuotedStringValue()
        {
            LanguageGrammar<string> g = new LanguageGrammar<string>();
            g.StringLiteralRule.Action = (s) => s;

            // Spin up a parser for our language.
            // TODO: Package this up and simplify it.
            var expressionHelper = new ExpressionHelper();
            //var classifierGen = new TerminalClassifier<char>();
            var classifierSession = new TerminalClassifierSession<char, ParserState, int>()
                .AddSkipTerminal(new Terminal<char> { Name = "Whitespace", InitialState = RegexCharNFABuilder.RegexCompiler(@"\s+") })
                .CurrentCharExprIs(x => x.CurrentChar())
                .GetFromMarkExprIs(x => x.GetFromMarkedPos())
                .HasCurrentCharExprIs(x => x.HasCurrentChar())
                .MarkPosExprIs(x => x.MarkPos())
                .MoveNextCharExprIs(x => x.MoveNextChar())
                .UnmarkPosExprIs(x => x.UnmarkPos());

            var parserGen = new ParserGenerator<char>(expressionHelper);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);
            var session = parserGen.NewSession<ParserState>()
                .NonTerminalValueExprIs<object>(x => x.NonTerminalValue)
                .TerminalValueExprIs<string>(x => x.TerminalStringValue)
                .TerminalValueExprIs<int>(x => x.TerminalIntValue)
                .TerminalValueExprIs<double>(x => x.TerminalDoubleValue)
                .TerminalIs(x => x.Terminal)
                .NonTerminalIs(x => x.NonTerminal)
                .IncludeSymbols(true)
                .UseDefaultValue(true)
                .DebugOutputIs(x => Debug.WriteLine(x))
                .Generate("LanguageParser", parseTable, classifierSession);
            // At this point, session is an Expression<ParserState,int> representing our parser.
            // We can compile it into a delegate or a MethodBuilder.  For the examples, we'll use a delegate.
            var compiler = session.Compile();

            ParserState ps = new ParserState("\"This is a quoted string\"");
            Assert.AreEqual(0, compiler(ps));
            Assert.AreEqual("This is a quoted string", ps.NonTerminalValue);

            ps = new ParserState("\"Here are some \\t escape characters\\r\\netc...\"");
            Assert.AreEqual(0, compiler(ps));
            Assert.AreEqual("Here are some \t escape characters\r\netc...", ps.NonTerminalValue);

            ps = new ParserState("\"And more \\\\ escape \\\" characters\"");
            Assert.AreEqual(0, compiler(ps));
            Assert.AreEqual("And more \\ escape \" characters", ps.NonTerminalValue);
        }

        [Test]
        public void TestParserProducesAValue()
        {
            // Create an instance of our language grammar and set handlers for its rules.
            LanguageGrammar<Expression> g = new LanguageGrammar<Expression>();

            g.DivideRule.Action = (e1, e2) => Expression.Divide(e1, e2);
            g.DoubleLiteralRule.Action = (d) => Expression.Constant(d);
            g.IntLiteralRule.Action = (i) => Expression.Constant(i);
            g.StringLiteralRule.Action = (s) => Expression.Constant(s);
            g.MinusRule.Action = (e1, e2) => Expression.Subtract(e1, e2);
            g.ModRule.Action = (e1, e2) => Expression.Modulo(e1, e2);
            g.MultiplyRule.Action = (e1, e2) => Expression.Multiply(e1, e2);
            g.NegateRule.Action = (e) => Expression.Negate(e);
            g.PlusRule.Action = (e1, e2) => Expression.Add(e1, e2);
            g.VariableRefRule.SetStatefulAction<ParserState>((s, name) => s.GetVariable(name));

            // Spin up a parser for our language.
            // TODO: Package this up and simplify it.
            var expressionHelper = new ExpressionHelper();
            var classifierSession = new TerminalClassifierSession<char, ParserState, int>()
                .AddSkipTerminal(new Terminal<char> {Name = "Whitespace", InitialState = RegexCharNFABuilder.RegexCompiler(@"\s+")})
                .CurrentCharExprIs(x => x.CurrentChar())
                .GetFromMarkExprIs(x => x.GetFromMarkedPos())
                .HasCurrentCharExprIs(x => x.HasCurrentChar())
                .MarkPosExprIs(x => x.MarkPos())
                .MoveNextCharExprIs(x => x.MoveNextChar())
                .UnmarkPosExprIs(x => x.UnmarkPos());

            var parserGen = new ParserGenerator<char>(expressionHelper);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);
            var session = parserGen.NewSession<ParserState>()
                .NonTerminalValueExprIs<object>(x => x.NonTerminalValue)
                .TerminalValueExprIs<string>(x => x.TerminalStringValue)
                .TerminalValueExprIs<int>(x => x.TerminalIntValue)
                .TerminalValueExprIs<double>(x => x.TerminalDoubleValue)
                .TerminalIs(x=>x.Terminal)
                .NonTerminalIs(x=>x.NonTerminal)
                .IncludeSymbols(true)
                .DebugOutputIs(x=>Debug.WriteLine(x))
                .Generate("LanguageParser", parseTable, classifierSession);
            // At this point, session is an Expression<ParserState,int> representing our parser.
            // We can compile it into a delegate or a MethodBuilder.  For the examples, we'll use a delegate.
            var compiler = session.Compile();

            // Create a parser state object and initialize it.
            ParserState ps = new ParserState("x*y + 2.0");
            ps.SetParameters(
                Expression.Parameter(typeof(double), "x"),
                Expression.Parameter(typeof(double), "y"));
            Assert.AreEqual(0, compiler(ps));
            Expression body = (Expression)ps.NonTerminalValue;
            Expression<Func<double, double, double>> lambda = Expression.Lambda<Func<double,double,double>>
                (body, ps.Variables["x"], ps.Variables["y"]);
            var f = lambda.Compile();
            Assert.AreEqual(12.0, f(5.0, 2.0));
            Assert.AreEqual(2.0, f(0.0, 1.0));
        }

        [Test]
        public void TestParseErrorGetsReported()
        {
            // Create an instance of our language grammar and set handlers for its rules.
            LanguageGrammar<Expression> g = new LanguageGrammar<Expression>();

            g.DivideRule.Action = (e1, e2) => Expression.Divide(e1, e2);
            g.DoubleLiteralRule.Action = (d) => Expression.Constant(d);
            g.IntLiteralRule.Action = (i) => Expression.Constant(i);
            g.StringLiteralRule.Action = (s) => Expression.Constant(s);
            g.MinusRule.Action = (e1, e2) => Expression.Subtract(e1, e2);
            g.ModRule.Action = (e1, e2) => Expression.Modulo(e1, e2);
            g.MultiplyRule.Action = (e1, e2) => Expression.Multiply(e1, e2);
            g.NegateRule.Action = (e) => Expression.Negate(e);
            g.PlusRule.Action = (e1, e2) => Expression.Add(e1, e2);
            g.VariableRefRule.SetStatefulAction<ParserState>((s, name) => s.GetVariable(name));

            // Spin up a parser for our language.
            // TODO: Package this up and simplify it.
            var expressionHelper = new ExpressionHelper();
            var classifierSession = new TerminalClassifierSession<char, ParserState, int>()
                .AddSkipTerminal(new Terminal<char> { Name = "Whitespace", InitialState = RegexCharNFABuilder.RegexCompiler(@"\s+") })
                .CurrentCharExprIs(x => x.CurrentChar())
                .GetFromMarkExprIs(x => x.GetFromMarkedPos())
                .HasCurrentCharExprIs(x => x.HasCurrentChar())
                .MarkPosExprIs(x => x.MarkPos())
                .MoveNextCharExprIs(x => x.MoveNextChar())
                .GetLocationIs(x=>new ParseLocation {Line = 1, Column = x.GetPos()})
                .ErrorCollectionIs(x=>x.Errors)
                .UnmarkPosExprIs(x => x.UnmarkPos());

            var parserGen = new ParserGenerator<char>(expressionHelper);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);
            var session = parserGen.NewSession<ParserState>()
                .NonTerminalValueExprIs<object>(x => x.NonTerminalValue)
                .TerminalValueExprIs<string>(x => x.TerminalStringValue)
                .TerminalValueExprIs<int>(x => x.TerminalIntValue)
                .TerminalValueExprIs<double>(x => x.TerminalDoubleValue)
                .TerminalIs(x => x.Terminal)
                .NonTerminalIs(x => x.NonTerminal)
                .IncludeSymbols(true)
                .DebugOutputIs(x => Debug.WriteLine(x))
                .Generate("LanguageParser", parseTable, classifierSession);
            // At this point, session is an Expression<ParserState,int> representing our parser.
            // We can compile it into a delegate or a MethodBuilder.  For the examples, we'll use a delegate.
            var compiler = session.Compile();

            // Create a parser state object and initialize it.
            ParserState ps = new ParserState("x*y +* 2.0");
            ps.SetParameters(
                Expression.Parameter(typeof(double), "x"),
                Expression.Parameter(typeof(double), "y"));
            Assert.AreNotEqual(0, compiler(ps));
            Assert.AreNotEqual(0, ps.Errors.Count);
            Assert.Less(ps.Errors.First().ExpectedTerminalNames.Count, g.Terminals.Count);

        }
    }
}
