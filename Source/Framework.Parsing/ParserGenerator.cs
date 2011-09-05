using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Framework.CodeGen;


namespace Framework.Parsing
{


    public class ParserGenerator<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        IExpressionHelper _expressionHelper;
        

        public ParserGenerator(IExpressionHelper expressionHelper)
        {
            _expressionHelper = expressionHelper;
        }


        public ISet<Terminal<TChar>> GetPossibleTerminals(IDictionary<Terminal<TChar>, int> allTerminals,
            LRParseState<TChar> nextState)
        {
            // TODO: Trace the actual set of possible terminals
            var result = new HashableSet<Terminal<TChar>>();
            result.UnionWith(allTerminals.Keys.Except(new[] {Eof<TChar>.Instance}));
            return result;
        }

        public ParserGeneratorSession<TChar> NewSession()
        {
            return new ParserGeneratorSession<TChar>(this, _expressionHelper);
        }

        public ParserGeneratorSession<TChar, TParseState> NewSession<TParseState>()
        {
            return new ParserGeneratorSession<TChar, TParseState>(this, _expressionHelper);
        }
    }

}
