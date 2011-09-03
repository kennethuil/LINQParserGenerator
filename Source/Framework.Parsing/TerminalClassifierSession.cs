using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Framework.Parsing
{
    public class TerminalClassifierSession<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        protected TerminalClassifier<TChar> _parserGenerator;
        protected Type _resultType;
        protected Type _parseStateType;
        protected LambdaExpression _hasCurrentChar;
        protected LambdaExpression _currentChar;
        protected LambdaExpression _moveNextChar;
        protected LambdaExpression _markPosition;
        protected LambdaExpression _getFromMark;
        protected LambdaExpression _unmarkPosition;
        protected IDictionary<Terminal<TChar>, LambdaExpression> _handlers;
        protected ISet<Terminal<TChar>> _capturingTerminals;
        protected ISet<Terminal<TChar>> _skipTerminals;
        protected LambdaExpression _rejectHandler;
        protected LambdaExpression _eofHandler;

        public TerminalClassifierSession(TerminalClassifier<TChar> pg, Type parseStateType, Type resultType)
        {
            _parserGenerator = pg;
            _handlers = new Dictionary<Terminal<TChar>, LambdaExpression>();
            _resultType = resultType;
            _parseStateType = parseStateType;
            _capturingTerminals = new HashSet<Terminal<TChar>>();
            _skipTerminals = new HashSet<Terminal<TChar>>();
        }

        public TerminalClassifier<TChar> Parent
        {
            get
            {
                return _parserGenerator;
            }
        }


        public Type StateType
        {
            get
            {
                return _parseStateType;
            }
        }

        public TerminalClassifierSession<TChar> HasCurrentCharExprIs(LambdaExpression x)
        {
            _hasCurrentChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar> CurrentCharExprIs(LambdaExpression x)
        {
            _currentChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar> MoveNextCharExprIs(LambdaExpression x)
        {
            _moveNextChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar> MarkPosExprIs(LambdaExpression x)
        {
            _markPosition = x;
            return this;
        }

        public TerminalClassifierSession<TChar> UnmarkPosExprIs(LambdaExpression x)
        {
            _unmarkPosition = x;
            return this;
        }

        public TerminalClassifierSession<TChar> GetFromMarkExprIs(LambdaExpression x)
        {
            _getFromMark = x;
            return this;
        }

        public TerminalClassifierSession<TChar> CreateWithNewResultType(Type t)
        {
            var created = new TerminalClassifierSession<TChar>(_parserGenerator, _parseStateType, t);
            created._capturingTerminals = new HashSet<Terminal<TChar>>(_capturingTerminals);
            created._currentChar = _currentChar;
            created._eofHandler = _eofHandler;
            created._getFromMark = _getFromMark;
            created._hasCurrentChar = _hasCurrentChar;
            created._markPosition = _markPosition;
            created._moveNextChar = _moveNextChar;
            created._rejectHandler = _rejectHandler;
            created._skipTerminals = new HashSet<Terminal<TChar>>(_skipTerminals);
            created._unmarkPosition = _unmarkPosition;
            return created;
        }

        protected bool HandlerNeedsCapture(LambdaExpression x)
        {
            return (x.Parameters.Count > 1);
        }

        public TerminalClassifierSession<TChar> AddTerminalHandler(Terminal<TChar> terminal, LambdaExpression x)
        {
            _handlers[terminal] = x;
            if (HandlerNeedsCapture(x))
            {
                _capturingTerminals.Add(terminal);
            }
            return this;
        }

        public LambdaExpression GetTerminalHandler(Terminal<TChar> terminal)
        {
            LambdaExpression result;
            _handlers.TryGetValue(terminal, out result);
            return result;
        }

        public TerminalClassifierSession<TChar> AddCapturingTerminal(Terminal<TChar> terminal)
        {
            _capturingTerminals.Add(terminal);
            return this;
        }

        public TerminalClassifierSession<TChar> AddSkipTerminal(Terminal<TChar> terminal)
        {
            _skipTerminals.Add(terminal);
            return this;
        }

        public TerminalClassifierSession<TChar> RejectHandlerIs(LambdaExpression x)
        {
            _rejectHandler = x;
            return this;
        }

        public TerminalClassifierSession<TChar> EofHandlerIs(LambdaExpression x)
        {
            _eofHandler = x;
            return this;
        }

        public bool IsCapturing(Terminal<TChar> term)
        {
            return _capturingTerminals.Contains(term);
        }

        LabelTarget GetStateTarget(FiniteAutomatonState<TChar> state, Dictionary<FiniteAutomatonState<TChar>, LabelTarget> stateTargets)
        {
            LabelTarget result;
            if (!stateTargets.TryGetValue(state, out result))
            {
                result = Expression.Label();
                stateTargets.Add(state, result);
            }
            return result;
        }

        bool IsCapturing(FiniteAutomatonState<TChar> state)
        {
            return _capturingTerminals.Intersect(state.PossibleTerminals).Count() > 0;
        }

        void AddBlock(
            ParameterExpression parseState,
            IDictionary<Terminal<TChar>, LambdaExpression> handlers,
            Dictionary<FiniteAutomatonState<TChar>, LabelTarget> stateTargets,
            Dictionary<FiniteAutomatonState<TChar>, BlockExpression> stateBlocks,
            FiniteAutomatonState<TChar> state,
            LabelTarget returnLabel,
            LabelTarget beginLabel)
        {
            LabelTarget thisTarget = GetStateTarget(state, stateTargets);

            BlockExpression stateBlock;
            if (stateBlocks.TryGetValue(state, out stateBlock))
                return;

            List<Expression> exprs = new List<Expression>();
            exprs.Add(Expression.Label(thisTarget));

            // NOTE: Using the first accepting terminal if present.
            var accepting = state.AcceptTerminals.FirstOrDefault();

            Expression noTransition;
            Expression reject = Expression.Goto(returnLabel, Expression.Invoke(_rejectHandler, parseState));

            if (accepting != null)
            {
                Expression accept = GetAccept(accepting, returnLabel, beginLabel, parseState, handlers);

                noTransition = accept;
            }
            else
                noTransition = reject;

            // Read another character and follow the right transition, or reject if no such transition exists.

            Expression onEof = noTransition;

            bool isCapturing = IsCapturing(state);

            // Go through the transitions and create the conditional jump expressions.
            List<Expression> jumps = new List<Expression>();
            var expCh = Expression.Parameter(typeof(TChar), "current");
            jumps.Add(Expression.Assign(expCh, Expression.Invoke(_currentChar, parseState)));
            var cases = new List<SwitchCase>();
            var positiveChars = new HashSet<TChar>();

            var inclusiveTransitions = from x in state.Transitions where !x.MatchAllExcept select x;

            foreach (var transition in inclusiveTransitions)
            {
                IEnumerable<Expression> all = GetTransitionExpression(stateTargets, parseState, transition, isCapturing);

                // On a character transition, consume the character and jump to the next state.
                if (!transition.MatchEof)
                {
                    cases.Add(Expression.SwitchCase(Expression.Block(all), from x in transition.Characters select Expression.Constant(x)));
                    positiveChars.UnionWith(transition.Characters);
                }
                else
                    onEof = Expression.Block(all);
            }

            var exclusiveTransitions = from x in state.Transitions where x.MatchAllExcept select x;
            if (exclusiveTransitions.Count() > 1)
            {
                throw new ApplicationException("Not supporting states with more than one outgoing transition having MatchAllExcept set to true");
            }
            var exclusiveTransition = exclusiveTransitions.FirstOrDefault();
            var defaultTransition = noTransition;
            
            if (exclusiveTransition != null)
            {
                IEnumerable<Expression> all = GetTransitionExpression(stateTargets, parseState, exclusiveTransition, isCapturing);
                var excludeChars = exclusiveTransition.Characters.Except(positiveChars);
                if (excludeChars.Count() > 0)
                {
                    cases.Add(Expression.SwitchCase(noTransition, from x in exclusiveTransition.Characters select Expression.Constant(x)));
                }
                defaultTransition = Expression.Block(all);
            }

            if (cases.Count > 0)
                jumps.Add(Expression.Switch(expCh, defaultTransition, cases.ToArray()));
            else
                jumps.Add(defaultTransition);
            
            var jumpBlock = Expression.Block(new ParameterExpression[] { expCh }, jumps);

            // The entire state block goes like this:
            // if (!hasCurrentChar(parseState))
            //   follow EOF transition or noTransition if there isn't one.
            // var current = currentChar(parseState);
            // if (transition1 character test)
            //    consume character
            //    goto transition1 state block
            // if (transition2 character test)
            //    consume character
            //    goto transition2 state block
            // ...
            // noTransition;
            //.
            // TODO: Add support for specifying specific characters in transitions, and rolling them
            // up into a switch statement here.
            stateBlock = Expression.Block(
                Expression.Label(thisTarget),
                Expression.IfThen(Expression.Not(Expression.Invoke(_hasCurrentChar, parseState)), onEof),
                jumpBlock,
                noTransition);

            stateBlocks.Add(state, stateBlock);
            foreach (var transition in state.Transitions)
            {
                AddBlock(parseState, handlers,
                    stateTargets, stateBlocks, transition.Target, returnLabel, beginLabel);
            }
        }

        private IEnumerable<Expression> GetTransitionExpression(Dictionary<FiniteAutomatonState<TChar>, LabelTarget> stateTargets, ParameterExpression parseState, FiniteAutomatonStateTransition<TChar> transition, bool isCapturing)
        {
            bool willBeCapturing = IsCapturing(transition.Target);

            var moveNextChar = new Expression[] { Expression.Invoke(_moveNextChar, parseState) };
            var jump = new Expression[] { Expression.Goto(GetStateTarget(transition.Target, stateTargets)) };

            return ((isCapturing && !willBeCapturing) ? new Expression[] { Expression.Invoke(_unmarkPosition, parseState) } : new Expression[0]).Concat(
                (transition.MatchEof ? new Expression[0] : moveNextChar)).Concat(
                    jump);
        }

        private Expression GetAccept(Terminal<TChar> accepting, LabelTarget returnLabel, LabelTarget beginLabel, ParameterExpression parseState,
            IDictionary<Terminal<TChar>, LambdaExpression> handlers)
        {
            Expression accept;

            // If there's a handler, call it and return its result here.  Otherwise just return a default result.

            LambdaExpression handler;
            if (handlers.TryGetValue(accepting, out handler))
            {
                Expression callHandler;
                if (handler.Parameters.Count == 1)
                {
                    callHandler = Expression.Invoke(handler, parseState);
                }
                else
                {
                    // TODO: More sophisticated conversions as needed.
                    callHandler = Expression.Invoke(handler, parseState,
                        Expression.Convert(Expression.Invoke(_getFromMark, parseState), handler.Parameters[1].Type));
                }
                accept = Expression.Goto(returnLabel, callHandler);
            }
            else if (_skipTerminals.Contains(accepting))
                accept = Expression.Goto(beginLabel);
            else
                accept = Expression.Goto(returnLabel);
            return accept;
        }

        public LambdaExpression Generate(ISet<Terminal<TChar>> terminals)
        {
            Dictionary<Terminal<TChar>, LambdaExpression> handlers = new Dictionary<Terminal<TChar>, LambdaExpression>();
            foreach (var terminal in terminals)
            {
                handlers.Add(terminal, _handlers[terminal]);
            }
            return Generate(handlers);
        }

        public LambdaExpression Generate(IDictionary<Terminal<TChar>, LambdaExpression> handlers)
        {
            FiniteAutomatonState<TChar> combined = _parserGenerator.CombineRecognizers(handlers.Keys.Concat(_skipTerminals).ToList());
            var stateParam = Expression.Parameter(_parseStateType, "parseState");
            // Now we have a DFA, turn it into a lambda expression.
            // Each state gets a label target.
            Dictionary<FiniteAutomatonState<TChar>, LabelTarget> stateTargets = new Dictionary<FiniteAutomatonState<TChar>, LabelTarget>();

            // And a block
            Dictionary<FiniteAutomatonState<TChar>, BlockExpression> stateBlocks = new Dictionary<FiniteAutomatonState<TChar>, BlockExpression>();

            LabelTarget returnLabel = Expression.Label(_resultType, "Return");
            LabelTarget beginLabel = Expression.Label(typeof(void), "Begin");

            // Crawl the graph and make the blocks.
            AddBlock(stateParam, handlers, stateTargets, stateBlocks, combined,
                returnLabel, beginLabel);

            var isCapturing = IsCapturing(combined);

            // Combine the blocks into one big block
            var body = Expression.Block(
                    (_eofHandler != null ? new Expression[] { 
                        Expression.IfThen(Expression.Not(Expression.Invoke(_hasCurrentChar, stateParam)), 
                            Expression.Goto(returnLabel, Expression.Invoke(_eofHandler, stateParam)))}
                        : new Expression[0]).Concat(
                    new Expression[] { Expression.Label(beginLabel) }).Concat(
                    (isCapturing ? new Expression[] { Expression.Invoke(_markPosition, stateParam) } : new Expression[] { })).Concat(
                    new Expression[] { stateBlocks[combined] }).Concat(
                    stateBlocks.Values.Except(new Expression[] { stateBlocks[combined] })).Concat(
                    new Expression[] { Expression.Label(returnLabel, Expression.Invoke(_rejectHandler, stateParam)) }));

            // And wrap it up in a lambda.
            var result = Expression.Lambda(body, true, stateParam);
            return result;
        }

        public LambdaExpression Generate()
        {
            return Generate(_handlers);
        }

    }

    public class TerminalClassifierSession<TChar, TParseState, THandlerResult> : TerminalClassifierSession<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public TerminalClassifierSession(TerminalClassifier<TChar> pg)
            : base(pg, typeof(TParseState), typeof(THandlerResult))
        {

        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> HasCurrentCharExprIs(LambdaExpression x)
        {
            _hasCurrentChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> HasCurrentCharExprIs(Expression<Func<TParseState, bool>> x)
        {
            _hasCurrentChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> CurrentCharExprIs(Expression<Func<TParseState, TChar>> x)
        {
            _currentChar = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> CurrentCharExprIs(LambdaExpression x)
        {
            _currentChar = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> MoveNextCharExprIs(Expression<Action<TParseState>> x)
        {
            _moveNextChar = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> MoveNextCharExprIs(LambdaExpression x)
        {
            _moveNextChar = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> MarkPosExprIs(LambdaExpression x)
        {
            _markPosition = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> UnmarkPosExprIs(LambdaExpression x)
        {
            _unmarkPosition = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> GetFromMarkExprIs(LambdaExpression x)
        {
            _getFromMark = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> MarkPosExprIs(Expression<Action<TParseState>> x)
        {
            _markPosition = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> UnmarkPosExprIs(Expression<Action<TParseState>> x)
        {
            _unmarkPosition = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> GetFromMarkExprIs(Expression<Func<TParseState,IEnumerable<TChar>>> x)
        {
            _getFromMark = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> AddTerminalHandler(Terminal<TChar> term, Expression<Func<TParseState, THandlerResult>> x)
        {
            _handlers[term] = x;
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> RejectHandlerIs(Expression<Func<TParseState, THandlerResult>> x)
        {
            _rejectHandler = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> AddTerminalHandler(Terminal<TChar> terminal, LambdaExpression x)
        {
            _handlers[terminal] = x;
            if (HandlerNeedsCapture(x))
            {
                _capturingTerminals.Add(terminal);
            }
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> AddSkipTerminal(Terminal<TChar> terminal)
        {
            _skipTerminals.Add(terminal);
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> RejectHandlerIs(LambdaExpression x)
        {
            _rejectHandler = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> EofHandlerIs(LambdaExpression x)
        {
            _eofHandler = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> EofHandlerIs(Expression<Func<TParseState,THandlerResult>> x)
        {
            _eofHandler = x;
            return this;
        }

        public new TerminalClassifierSession<TChar, TParseState, THandlerResult> AddCapturingTerminal(Terminal<TChar> terminal)
        {
            _capturingTerminals.Add(terminal);
            return this;
        }

        public TerminalClassifierSession<TChar, TParseState, T> CreateWithNewResultType<T>()
        {
            var t = typeof(T);

            var created = new TerminalClassifierSession<TChar, TParseState, T>(_parserGenerator);
            created._capturingTerminals = new HashSet<Terminal<TChar>>(_capturingTerminals);
            created._currentChar = _currentChar;
            created._eofHandler = _eofHandler;
            created._getFromMark = _getFromMark;
            created._hasCurrentChar = _hasCurrentChar;
            created._markPosition = _markPosition;
            created._moveNextChar = _moveNextChar;
            created._rejectHandler = _rejectHandler;
            created._skipTerminals = new HashSet<Terminal<TChar>>(_skipTerminals);
            created._unmarkPosition = _unmarkPosition;
            return created;
        }

        public new Expression<Func<TParseState, THandlerResult>> Generate()
        {
            return (Expression<Func<TParseState, THandlerResult>>)base.Generate();
        }

        public Expression<Func<TParseState, THandlerResult>> Generate(IDictionary<Terminal<TChar>, Expression<Func<TParseState, THandlerResult>>> handlers)
        {
            var converted = handlers.ToDictionary(x => x.Key, x => (LambdaExpression)x.Value);
            return (Expression<Func<TParseState, THandlerResult>>)base.Generate(converted);

        }
    }
}
