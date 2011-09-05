using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using ExpressionTests;
using Framework.CodeGen;


namespace Framework.Parsing
{
    public class ParserGeneratorSession<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        ParserGenerator<TChar> _parserGenerator;
        IExpressionHelper _expressionHelper;
        protected bool _includeSymbols;
        Dictionary<ISet<Terminal<TChar>>, MethodInfo> _terminalReaders = new Dictionary<ISet<Terminal<TChar>>, MethodInfo>();

        public ParserGeneratorSession(ParserGenerator<TChar> pg, IExpressionHelper expressionHelper)
        {
            _parserGenerator = pg;
            _expressionHelper = expressionHelper;
        }

        public LambdaExpression GetTerminal
        {
            get;
            protected set;
        }

        public LambdaExpression SetTerminal
        {
            get;
            protected set;
        }

        public LambdaExpression GetNonTerminal
        {
            get;
            protected set;
        }

        public LambdaExpression SetNonTerminal
        {
            get;
            protected set;
        }

        public Func<Type, LambdaExpression> GetTerminalValueExpr
        {
            get;
            protected set;
        }

        public Func<Type, LambdaExpression> SetTerminalValueExpr
        {
            get;
            protected set;
        }

        public Func<Type, LambdaExpression> GetNonTerminalValueExpr
        {
            get;
            protected set;
        }

        public Func<Type, LambdaExpression> SetNonTerminalValueExpr
        {
            get;
            protected set;
        }

        public ParserGeneratorSession<TChar> GetTerminalIs(LambdaExpression x)
        {
            GetTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar> SetTerminalIs(LambdaExpression x)
        {
            SetTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar> GetTerminalValueExprIs(Func<Type,LambdaExpression> x)
        {
            GetTerminalValueExpr = x;
            return this;
        }

        public ParserGeneratorSession<TChar> SetTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            SetTerminalValueExpr = x;
            return this;
        }

        public ParserGeneratorSession<TChar> GetNonTerminalIs(LambdaExpression x)
        {
            GetNonTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar> SetNonTerminalIs(LambdaExpression x)
        {
            SetNonTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar> GetNonTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            GetNonTerminalValueExpr = x;
            return this;
        }

        public ParserGeneratorSession<TChar> SetNonTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            SetNonTerminalValueExpr = x;
            return this;
        }

        public ParserGeneratorSession<TChar> IncludeSymbols(bool include)
        {
            _includeSymbols = include;
            return this;
        }

        private Expression GetReadTerminal(Expression stateParam, 
            TerminalClassifierSession<TChar> classifier, 
            IDictionary<Terminal<TChar>, int> allTerminals, 
            LRParseState<TChar> targetState,
            TypeBuilder parserType)
        {
            if (parserType != null)
            {
                var possibleTerminals = _parserGenerator.GetPossibleTerminals(allTerminals, targetState);
                MethodInfo method;
                if (!_terminalReaders.TryGetValue(possibleTerminals, out method))
                {
                    var code = classifier.Generate(possibleTerminals);
                    var mbwrap = new MethodBuilderWrap(parserType, "ScanToken" + _terminalReaders.Count, MethodAttributes.Static | MethodAttributes.Public,
                        typeof(int), stateParam.Type);
                    var mb = mbwrap.GetMethodBuilder();
                    code.CompileToMethod(mb);
                    method = mbwrap;
                    _terminalReaders.Add(possibleTerminals, method);
                }
                
                return Expression.Call(method, stateParam);
            }
            return Expression.Invoke(classifier.Generate(_parserGenerator.GetPossibleTerminals(allTerminals, targetState)), stateParam);
        }

        IEnumerable<Expression> GetOutgoingParamList(Expression stateParam, Expression depthParam,
            ParameterExpression[] stackValueParams,
            Expression expPushedValue)
        {
            
            // If there is a pushed value, the outgoing list should be the result of the push, otherwise the
            // outgoing list is the same as the incoming list.
            IEnumerable<Expression> outgoingStackValues =
                expPushedValue == null ? stackValueParams :
                new Expression[] { Expression.Convert(expPushedValue, stackValueParams.First().Type) }.Concat(stackValueParams.Take(stackValueParams.Length - 1));

            return new Expression[] { stateParam, Expression.Add(depthParam, Expression.Constant(1)) }.Concat(outgoingStackValues);
        }

        Expression GenerateShiftWithValue(TerminalClassifierSession<TChar> classifier, LRParseState<TChar> targetState, MethodInfo callTarget, Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            Terminal<TChar> term, IDictionary<Terminal<TChar>, int> allTerminals, TypeBuilder parserTypeBuilder)
        {
            // Generate code to represent a "shift" action where the terminal has an associated value.  The terminal's value is pushed onto the value stack.
            var expTermValue = Expression.Parameter(term.ValueType);

            var termValueAssign = Expression.Assign(expTermValue, Expression.Invoke(this.GetTerminalValueExpr(term.ValueType), stateParam));
            var readTerminal = GetReadTerminal(stateParam, classifier, allTerminals, targetState, parserTypeBuilder);

            var result = Expression.Call(callTarget, GetOutgoingParamList(stateParam, depthParam, stackValueParams, expTermValue));
            return Expression.Block(
                new ParameterExpression[] { expTermValue },
                termValueAssign, readTerminal, result);
        }

        Expression GenerateShiftWithoutValue(TerminalClassifierSession<TChar> classifier, LRParseState<TChar> targetState, MethodInfo callTarget, Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            Terminal<TChar> term, IDictionary<Terminal<TChar>, int> allTerminals, TypeBuilder parserTypeBuilder)
        {
            // Generate code to represent a "shift" action where the terminal does not have an associated value.  The value stack remains unchanged.
            var readTerminal = GetReadTerminal(stateParam, classifier, allTerminals, targetState, parserTypeBuilder);
            var result = Expression.Call(callTarget, GetOutgoingParamList(stateParam, depthParam, stackValueParams, null));
            return Expression.Block(readTerminal, result);
        }

        Expression GenerateShift(TerminalClassifierSession<TChar> classifier, LRParseState<TChar> targetState, MethodInfo callTarget, Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            Terminal<TChar> term, IDictionary<Terminal<TChar>, int> allTerminals, TypeBuilder parserTypeBuilder)
        {
            // Generate code to represent a "shift" action
            //bool hasValue = classifier.IsCapturing(term);
            bool hasValue = term.ValueType != null && term.ValueType != typeof(void);

            return hasValue ? GenerateShiftWithValue(classifier, targetState, callTarget, stateParam, depthParam, stackValueParams,
                term, allTerminals, parserTypeBuilder)
                : GenerateShiftWithoutValue(classifier, targetState, callTarget, stateParam, depthParam, stackValueParams,
                term, allTerminals, parserTypeBuilder);
        }

        Expression GenerateReduce(Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            GrammarRule rule, IDictionary<NonTerminal,int> allNonTerminals)
        {
            // Generate code to represent a "reduce" action
            NonTerminal nt = rule.LeftHandSide;
            var statements = new List<Expression>();

            // Set the current nonterminal.  We will need this to pick the next state in our "goto" code.
            statements.Add(Expression.Invoke(this.SetNonTerminal, stateParam, Expression.Constant(allNonTerminals[nt])));

            // If the rule has a semantic action, generate code to call it.
            if (rule.Action != null)
            {
                var actionParams = new List<Expression>();
                int numStateParams;

                if (rule.Action.Parameters[0].Type == stateParam.Type)
                {
                    actionParams.Add(stateParam);
                    numStateParams = 1;
                }
                else
                    numStateParams = 0;
                int numValueParams = rule.Action.Parameters.Count - numStateParams;
                int i;
                for (i = 0; i < numValueParams; ++i)
                {
                    // TODO: Find a way to get the states to take the types that the actions need, or at least cut down on the amount of boxing & unboxing.
                    actionParams.Add(Expression.Convert(stackValueParams[(numValueParams-1) -i], rule.Action.Parameters[i+numStateParams].Type));
                }

                var invoke = Expression.Invoke(rule.Action, actionParams);
                Expression callAction;
                if (invoke.Type != typeof(void) &&
                    rule.LeftHandSide.ValueType != typeof(void) && rule.LeftHandSide.ValueType != null)
                    callAction = Expression.Invoke(this.SetNonTerminalValueExpr(invoke.Type), stateParam, invoke);
                else
                    callAction = invoke;

                statements.Add(callAction);
            }
            else if (rule.LeftHandSide.ValueType != typeof(void) && rule.LeftHandSide.ValueType != null)
            {
                // Nonterminal has a value type but no action, so we'll treat it as a pass-through.
                // That is, the current non-terminal value is whatever is on top of the value stack at this point.
                statements.Add(Expression.Invoke(this.SetNonTerminalValueExpr(rule.LeftHandSide.ValueType), stateParam, 
                    Expression.Convert(stackValueParams[0], rule.LeftHandSide.ValueType)));
            }
            // The execution stack needs to have one frame popped for each symbol on the right hand side of the rule.
            int depthChange = rule.RightHandSide.Count;
            statements.Add(Expression.Subtract(depthParam, Expression.Constant(depthChange)));

            return Expression.Block(statements);
        }


        void GenerateState(MethodBuilder stateMethodBuilder, int numValueParams, LRParseState<TChar> state,
            IDictionary<LRParseState<TChar>, MethodInfo> callTargets,
            TerminalClassifierSession<TChar> classifier,
            IDictionary<Terminal<TChar>, int> allTerminals,
            IDictionary<NonTerminal, int> allNonTerminals,
            TypeBuilder parserTypeBuilder)
        {
            // A state method takes as parameters the parser state, current parser stack depth, and the value stack, and returns the stack depth that the
            // parsing stack frame should have after this state's reductions and gotos are done.
            var stateParam = Expression.Parameter(classifier.StateType, "state");
            var depthParam = Expression.Parameter(typeof(int), "depth");
            var stackParams = new ParameterExpression[numValueParams];
            var reductionRule = Expression.Parameter(typeof(string), "reductionRule");

            int i;
            for (i = 0; i < numValueParams; ++i)
            {
                stackParams[i] = Expression.Parameter(typeof(object), "stack" + i);
            }

            List<SwitchCase> cases = new List<SwitchCase>();
            IDictionary<GrammarRule, List<int>> reductionRules = new Dictionary<GrammarRule, List<int>>();

            foreach (var actionEntry in state.Actions)
            {
                var term = actionEntry.Key;
                var termNumber = allTerminals[term];

                var actionSet = actionEntry.Value;

                // TODO: If there's multiple actions here, codegen in some GLR goodness.
                if (actionSet.Count() > 1)
                {
                    throw new NotSupportedException("LALR conflict detected");
                }
                var action = actionSet.FirstOrDefault();
                if (action == null)
                    continue;

                if (action is ShiftAction<TChar>)
                {
                    var targetState = ((ShiftAction<TChar>)action).TargetState;
                    var callTarget = callTargets[targetState];
                    var shift = GenerateShift(classifier, targetState, callTarget, stateParam, depthParam, stackParams,
                        term, allTerminals, parserTypeBuilder);

                    cases.Add(Expression.SwitchCase(shift, Expression.Constant(termNumber)));
                }
                else if (action is ReduceAction<TChar>)
                {
                    var reduceAction = (ReduceAction<TChar>)action;
                    var rule = reduceAction.ReductionRule;
                    List<int> values;
                    if (!reductionRules.TryGetValue(rule, out values))
                    {
                        values = new List<int>();
                        reductionRules.Add(rule, values);
                    }
                    else
                    {
                    }
                    values.Add(termNumber);
                }
                else if (action is AcceptAction<TChar>)
                {
                    // When reaching an accept, all parsing should stop, so it should return to parser stack frame zero.
                    cases.Add(Expression.SwitchCase(Expression.Constant(0), Expression.Constant(termNumber)));
                }
            }
            foreach (var entry in reductionRules)
            {
                var rule = entry.Key;
                var reduce = GenerateReduce(stateParam, depthParam, stackParams, rule, allNonTerminals);
                if (_includeSymbols)
                    reduce = Expression.Block(
                        Expression.Assign(reductionRule, Expression.Constant(rule.ToString())),
                        reduce);
                var values = entry.Value;
                cases.Add(Expression.SwitchCase(reduce, from x in values select Expression.Constant(x)));
            }
            
            var expTerm = Expression.Invoke(this.GetTerminal, stateParam);
            Expression shiftOrReduce = Expression.Switch(
                expTerm,
                Expression.Constant(-1),
                cases.ToArray());

            Expression body;

            if (state.Goto != null && state.Goto.Count != 0)
            {
                List<Expression> parts = new List<Expression>();
                ParameterExpression expNewDepth = Expression.Parameter(typeof(int), "x");
                var assignNewDepth = Expression.Assign(expNewDepth, shiftOrReduce);

                List<SwitchCase> gotoCases = new List<SwitchCase>();
                foreach (var g in state.Goto)
                {
                    var nt = g.Key;
                    var targetState = g.Value;
                    var ntNumber = allNonTerminals[nt];

                    Expression ntValue = null;
                    if (nt.ValueType != null && nt.ValueType != typeof(void))
                    {
                        ntValue = Expression.Invoke(this.GetNonTerminalValueExpr(nt.ValueType), stateParam);
                    }
                    var targetMethod = callTargets[targetState];
                    var doGoto = Expression.Call(targetMethod, GetOutgoingParamList(stateParam, depthParam, stackParams, ntValue));
                    gotoCases.Add(Expression.SwitchCase(doGoto, new[] { Expression.Constant(ntNumber) }));
                }
                LabelTarget label = Expression.Label(typeof(void));
                Expression gotoLoop = Expression.Loop(
                    Expression.Block(
                        Expression.IfThen(Expression.NotEqual(depthParam, expNewDepth), Expression.Break(label)),
                        Expression.Assign(
                            expNewDepth,
                            Expression.Switch(
                                Expression.Invoke(this.GetNonTerminal, stateParam), Expression.Constant(-1), gotoCases.ToArray()))));

                body = Expression.Block(new[] { expNewDepth },
                    assignNewDepth,
                    gotoLoop,
                    Expression.Label(label),
                    expNewDepth);

            }
            else
            {
                body = shiftOrReduce;
            }

            if (_includeSymbols)
                body = Expression.Block(
                    new[] { reductionRule }, body);

            var lambda = Expression.Lambda(body, true, new ParameterExpression[] { stateParam, depthParam }.Concat(stackParams));
            if (_includeSymbols)
                lambda.CompileToMethod(stateMethodBuilder, DebugInfoGenerator.CreatePdbGenerator());
            else
                lambda.CompileToMethod(stateMethodBuilder);
        }

        public LambdaExpression Generate(string prefix, LRParseTable<TChar> parseTable, TerminalClassifierSession<TChar> classifier)
        {
            var assmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("GeneratedParser"), AssemblyBuilderAccess.RunAndCollect);
            var modBuilder = assmBuilder.DefineDynamicModule("GeneratedParser.dll", _includeSymbols);
            var typeBuilder = modBuilder.DefineType(prefix + "ExpressionParser", TypeAttributes.Public);
            var result = Generate(prefix, modBuilder, typeBuilder, parseTable, classifier);
            typeBuilder.CreateType();
            return result;
        }

        public LambdaExpression Generate(string prefix, ModuleBuilder parserModuleBuilder, TypeBuilder parserTypeBuilder,
            LRParseTable<TChar> parseTable,
            TerminalClassifierSession<TChar> classifier)
        {
            // Find out the maximum number of values that need to be passed to any nonterminal rule action
            // Each state method will need that number of stack value parameters.
            int numValueParams = parseTable.Rules.Where(rule => rule.Action != null).Aggregate(0, (current, rule) => Math.Max(current, rule.Action.Parameters.Count));

            classifier = classifier.CreateWithNewResultType(typeof(int));

            // Set up the method builders that we'll populate with lambdas.  They have to call each other mutually recursively, so
            // we have to create a separate type with passthrough calls back to the MethodBuilders, since lambda expression trees
            // can't refer to method builders (as of .NET 4)
            IDictionary<LRParseState<TChar>, MethodBuilder> stateMethodBuilders = new Dictionary<LRParseState<TChar>, MethodBuilder>();
            IDictionary<LRParseState<TChar>, MethodInfo> stateMethodCallTargets = new Dictionary<LRParseState<TChar>, MethodInfo>();
            int i = 0;

            // A table of terminals to unique terminal numbers, for use in state method switch statements.
            IDictionary<Terminal<TChar>, int> allTerminals = new Dictionary<Terminal<TChar>, int>();
            

            // A table of nonterminals to unique nonterminal numbers, for use in state method switch statements.
            IDictionary<NonTerminal, int> allNonTerminals = new Dictionary<NonTerminal, int>();

            // The complete list of parameter types for all state methods.
            var stateParameterTypes = new Type[] { classifier.StateType, typeof(int) }.Concat(
                Enumerable.Repeat(typeof(object), numValueParams)).ToArray();

            // Loop through all states in our parse table, and set up terminal handlers in the terminal classifier,
            // and create a MethodBuilder for the body of each state method.
            // Here we initialize some stuff for the loop
            int terminalNumber = 1;
            ParameterExpression expParserState = Expression.Parameter(classifier.StateType);

            // EOF never produces a value on the parsing value stack.  EOF is assigned terminal number 0.
            classifier.EofHandlerIs(Expression.Lambda(Expression.Block(
                                            Expression.Invoke(SetTerminal, expParserState, Expression.Constant(0)),
                                            Expression.Constant(0)),
                                        true, expParserState));

            classifier.RejectHandlerIs(Expression.Lambda(Expression.Block(
                                            Expression.Invoke(SetTerminal, expParserState, Expression.Constant(-1)),
                                            Expression.Constant(-1)),
                                        true, expParserState));
            allTerminals.Add(Eof<TChar>.Instance, 0);

            // The big loop itself.
            foreach (var state in parseTable.States)
            {
                // MethodBuilder for the code to handle the state
                var methodBuilder = parserTypeBuilder.DefineMethod("State" + i, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), stateParameterTypes);
                ++i;
                stateMethodBuilders[state] = methodBuilder;

                // Check all the terminals in the state's actions.  If any haven't been processed before,
                // process them here.
                foreach (var action in state.Actions)
                {
                    int existing;
                    if (allTerminals.TryGetValue(action.Key, out existing)) continue;

                    // Assign it a terminal number.
                    var term = action.Key;
                    allTerminals.Add(action.Key, terminalNumber);

                    // Set the classifier's handler.

                    // If the terminal produces a parse value, expTerminalString will be passed in
                    // to the classifier's handler, and expValue will be the resulting parse value.
                    ParameterExpression expTerminalString = null;
                    Expression expValue = null;
                    if (term.Action != null)
                    {
                        if (term.Action.Parameters.Count() == 0)
                        {
                            expValue = Expression.Invoke(term.Action);
                        }
                        else if (term.Action.Parameters.Count() == 1)
                        {
                            expTerminalString = Expression.Parameter(term.Action.Parameters[0].Type);
                            expValue = Expression.Invoke(term.Action, expTerminalString);
                        }
                        else
                        {
                            expTerminalString = Expression.Parameter(term.Action.Parameters[1].Type);
                            expValue = Expression.Invoke(term.Action, expParserState, expTerminalString);
                        }

                    }
                    else if (term.ValueType != null && term.ValueType != typeof(void))
                    {
                        expTerminalString = Expression.Parameter(term.ValueType);
                        expValue = expTerminalString;
                    }

                    // If there's a parse value, the terminal handler in the classifier needs to set the current terminal parse value.
                    // In any case, it needs to return the terminal number assigned to this terminal.
                    if (expValue != null)
                    {
                        List<ParameterExpression> handlerParams = new List<ParameterExpression> { expParserState };
                        if (expTerminalString != null)
                            handlerParams.Add(expTerminalString);

                        classifier.AddTerminalHandler(term,
                                                      Expression.Lambda(
                                                          Expression.Block(
                                                              Expression.Invoke(SetTerminalValueExpr(expValue.Type), expParserState, expValue),
                                                              Expression.Invoke(SetTerminal, expParserState, Expression.Constant(terminalNumber)),
                                                              Expression.Constant(terminalNumber)), true, handlerParams));
                    }
                    else if (term != Eof<TChar>.Instance)
                        classifier.AddTerminalHandler(term,
                                                      Expression.Lambda(
                                                          Expression.Block(
                                                            Expression.Invoke(SetTerminal, expParserState, Expression.Constant(terminalNumber)),
                                                            Expression.Constant(terminalNumber)),
                                                          true, expParserState));

                    // No other terminal should get this terminal number.
                    ++terminalNumber;
                }
            }

            // Each nonterminal that the parse table recognizes should get its own nonterminal number for use in switch statements.
            int nonTerminalNumber = 1;
            foreach (var rule in parseTable.Rules)
            {
                int existing;
                if (!allNonTerminals.TryGetValue(rule.LeftHandSide, out existing))
                {
                    allNonTerminals.Add(rule.LeftHandSide, nonTerminalNumber);
                    ++nonTerminalNumber;
                }
            }

            // Build the passthrough methods so that we have fully baked methods for the state methods to call as needed.
            // We need one for each state, and each should call the associated state method.
            var passthruTypeBuilder = parserModuleBuilder.DefineType(prefix + "-PassthruCalls", TypeAttributes.Public);
            i = 0;
            foreach (var state in parseTable.States)
            {
                var methodBuilder = passthruTypeBuilder.DefineMethod("CallState" + i, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), stateParameterTypes);
                _expressionHelper.PopulatePassthru(methodBuilder, stateMethodBuilders[state], stateParameterTypes.Length);
                ++i;
            }
            var passthruType = passthruTypeBuilder.CreateType();
            i = 0;
            foreach (var state in parseTable.States)
            {
                var method = passthruType.GetMethod("CallState" + i, BindingFlags.Public | BindingFlags.Static);
                stateMethodCallTargets[state] = method;
                ++i;
            }

            // Now we can start generating code for each state method.
            foreach (var state in parseTable.States)
            {
                var methodBuilder = stateMethodBuilders[state];
                GenerateState(methodBuilder, numValueParams, state, stateMethodCallTargets, classifier, allTerminals, allNonTerminals, parserTypeBuilder);
            }

            // Now to build the lambda that kicks off the entire parsing process.
            // It needs to read the first terminal, and then call the start state.
            var startMethod = stateMethodCallTargets[parseTable.States.First()];

            // Read the first terminal
            var shift = GetReadTerminal(expParserState, classifier, allTerminals, parseTable.States.First(), null);
            // Call the state method for the state state.  Start with a depth of one.  Then depth 0 or -1 should then cause the start state method to
            // return here.
            var start = Expression.Call(startMethod, new Expression[] {expParserState, Expression.Constant(1)}.Concat(
                Enumerable.Repeat(Expression.Constant(null, typeof(object)), numValueParams)));

            // And build the lambda.
            // NOTE: If the entire parse is successful, the return value will be zero, otherwise it will be -1.
            var body = Expression.Block(shift, start);
            return Expression.Lambda(body, true, expParserState);
        }
    }

    public class ParserGeneratorSession<TChar, TParseState> : ParserGeneratorSession<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public ParserGeneratorSession(ParserGenerator<TChar> pg, IExpressionHelper expressionHelper) : base(pg, expressionHelper)
        {
        }

        public new ParserGeneratorSession<TChar, TParseState> GetTerminalIs(Expression<Func<TParseState, int>> x)
        {
            GetTerminal = x;
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> SetTerminalIs(Expression<Action<TParseState, int>> x)
        {
            SetTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> TerminalIs(Expression<Func<TParseState, int>> x)
        {
            var body = x.Body;
            var pState = x.Parameters[0];
            var pVal = Expression.Parameter(typeof(int), "val");
            GetTerminalIs(x);
            SetTerminalIs(Expression.Lambda(Expression.Assign(body, pVal), true, pState, pVal));
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> GetTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            GetTerminalValueExpr = x;
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> SetTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            SetTerminalValueExpr = x;
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> TerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            GetTerminalValueExpr = x;
            SetTerminalValueExpr = (t) =>
                {
                    var assignable = x(t);
                    var body = assignable.Body;
                    var pState = assignable.Parameters[0];
                    var pVal = Expression.Parameter(t, "val");
                    if (body is UnaryExpression && body.NodeType == ExpressionType.Convert)
                    {
                        var operand = ((UnaryExpression)body).Operand;
                        return Expression.Lambda(Expression.Assign(operand, Expression.Convert(
                            pVal, operand.Type)), true, pState, pVal);
                    }

                    return Expression.Lambda(Expression.Assign(body, pVal), true, pState, pVal);
                };
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> TerminalValueExprIs<TVal>(Expression<Func<TParseState, TVal>> x)
        {
            var existing = GetTerminalValueExpr;
            if (existing == null)
                existing = t => null;

            TerminalValueExprIs((t) =>
                {
                    return t == typeof(TVal) ? x : existing(t);
                });
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> NonTerminalValueExprIs<TVal>(Expression<Func<TParseState, TVal>> x)
        {
            var existing = GetNonTerminalValueExpr;
            if (existing == null)
                existing = t => null;

            NonTerminalValueExprIs((t) =>
                t == typeof(TVal) ? x : existing(t));
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> NonTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            GetNonTerminalValueExpr = x;
            SetNonTerminalValueExpr = (t) =>
            {
                var assignable = x(t);
                var body = assignable.Body;
                var pState = assignable.Parameters[0];
                var pVal = Expression.Parameter(t, "val");
                if (body is UnaryExpression && body.NodeType == ExpressionType.Convert)
                {
                    var operand = ((UnaryExpression)body).Operand;
                    return Expression.Lambda(Expression.Assign(operand, Expression.Convert(
                        pVal, operand.Type)), true, pState, pVal);
                }
                return Expression.Lambda(Expression.Assign(body, pVal), true, pState, pVal);
            };
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> GetNonTerminalIs(Expression<Func<TParseState,int>> x)
        {
            GetNonTerminal = x;
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> SetNonTerminalIs(Expression<Action<TParseState, int>> x)
        {
            SetNonTerminal = x;
            return this;
        }

        public ParserGeneratorSession<TChar, TParseState> NonTerminalIs(Expression<Func<TParseState, int>> x)
        {
            var body = x.Body;
            var pState = x.Parameters[0];
            var pVal = Expression.Parameter(typeof(int), "val");
            GetNonTerminalIs(x);
            SetNonTerminalIs(Expression.Lambda(Expression.Assign(body, pVal), true, pState, pVal));
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> GetNonTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            GetNonTerminalValueExpr = x;
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> SetNonTerminalValueExprIs(Func<Type, LambdaExpression> x)
        {
            SetNonTerminalValueExpr = x;
            return this;
        }

        public new ParserGeneratorSession<TChar, TParseState> IncludeSymbols(bool include)
        {
            _includeSymbols = include;
            return this;
        }

        public new Expression<Func<TParseState,int>> Generate(string prefix, ModuleBuilder parserModuleBuilder, TypeBuilder parserTypeBuilder,
            LRParseTable<TChar> parseTable,
            TerminalClassifierSession<TChar> classifier)
        {
            return (Expression<Func<TParseState, int>>)base.Generate(prefix, parserModuleBuilder, parserTypeBuilder, parseTable, classifier);
        }

        public new Expression<Func<TParseState, int>> Generate(string prefix, LRParseTable<TChar> parseTable,
            TerminalClassifierSession<TChar> classifier)
        {
            return (Expression<Func<TParseState, int>>)base.Generate(prefix, parseTable, classifier);
        }
    }
}
