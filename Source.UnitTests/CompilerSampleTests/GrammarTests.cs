using System;
using System.Collections.Generic;
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
        [Test]
        public void TestParserProducesAValue()
        {
            LanguageGrammar<Expression> g = new LanguageGrammar<Expression>();
            var expressionHelper = new ExpressionHelper();
            var classifierGen = new TerminalClassifier<char>(expressionHelper);

        }
    }
}
