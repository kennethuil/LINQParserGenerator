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
        Terminal<char, string> Identifier = new Terminal<char, string> { Name = "Identifier", InitialState = _regexCompiler("[A-Za-z][A-Za-z0-9]") };
        NonTerminal<TValue> VariableReference = new NonTerminal<TValue>();
        private GrammarRule<string, TValue> VariableRefRule;

        public LanguageGrammar()
        {
            var rules = new GrammarRule[] {
                VariableRefRule = GrammarRule<string, TValue>.Create(VariableReference, Identifier)
            };
            Init(rules);
        }
    }
}
