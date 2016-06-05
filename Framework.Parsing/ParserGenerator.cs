﻿using System;
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

        /// <summary>
        /// Get all of the terminals that can possibly be valid when shifting to the LR(1) state nextState
        /// </summary>
        /// <param name="nextState">The state being shifted to after consuming the next terminal</param>
        /// <returns>The set of all terminals that can possibly be valid when shifting to nextState</returns>
        public ISet<Terminal<TChar>> GetPossibleTerminals(LRParseState<TChar> nextState)
        {
            var result = new HashableSet<Terminal<TChar>>();
            foreach (var entry in nextState.Actions)
            {
                var term = entry.Key;
                if (term != Eof<TChar>.Instance)
                    result.Add(term);
            }
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