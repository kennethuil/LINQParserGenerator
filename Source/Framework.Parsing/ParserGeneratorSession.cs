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

        /// <summary>
        /// Returns an Expression that reads the next terminal from the input stream
        /// </summary>
        /// <param name="stateParam">An Expression for the parser state object</param>
        /// <param name="classifier">The classifier generator</param>
        /// <param name="allTerminals">All the terminals in the grammar</param>
        /// <param name="targetState">The state that will run immediately after the next terminal is read.</param>
        /// <param name="parserType">A builder for the parser class.</param>
        /// <returns></returns>
        private Expression GetReadTerminal(Expression stateParam, 
            TerminalClassifierSession<TChar> classifier, 
            IDictionary<Terminal<TChar>, int> allTerminals, 
            LRParseState<TChar> targetState,
            TypeBuilder parserType)
        {
            if (parserType != null)
            {
                var possibleTerminals = _parserGenerator.GetPossibleTerminals(allTerminals, targetState);
                // See if there's already a read method for this set of possible terminals.  If not, create a new one.
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
                // Put in a call to the read method.
                return Expression.Call(method, stateParam);
            }
            // No type builder, so we have nowhere to put the read method.  Inline it here.
            // NOTE: Don't make a habit of it, or state methods will get rather large.
            return Expression.Invoke(classifier.Generate(_parserGenerator.GetPossibleTerminals(allTerminals, targetState)), stateParam);
        }

        /// <summary>
        /// Get the list of parameters with which to call the next state.
        /// </summary>
        /// <param name="stateParam">An Expression for the parser state object</param>
        /// <param name="depthParam">An Expression for the parsing depth parameter</param>
        /// <param name="stackValueParams">Expressions for the current set of stack values</param>
        /// <param name="expPushedValue">An Expression for the new value to go on the stack, or null if the stack should be unchanged</param>
        /// <returns>The list of parameters with which to call the next state.</returns>
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

        /// <summary>
        /// Create an expression that represents an LR(1) "shift" operation, in the case where the terminal has an associated value.
        /// </summary>
        /// <param name="classifier">The classifier generator</param>
        /// <param name="targetState">The target state of the shift operation</param>
        /// <param name="callTarget">The method representing the target state</param>
        /// <param name="stateParam">An Expression representing the parser state object</param>
        /// <param name="depthParam">An Expression representing the parse depth</param>
        /// <param name="stackValueParams">Expressions representing the values on the stack</param>
        /// <param name="term">The terminal which needs to be matched in order for this shift to take place</param>
        /// <param name="allTerminals">All terminals in the grammar</param>
        /// <param name="parserTypeBuilder">The type builder for the parser class</param>
        /// <returns>An expression that represents an LR(1) "shift" operation</returns>
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

        /// <summary>
        /// Create an expression that represents an LR(1) "shift" operation, in the case where the terminal does not have an associated value.
        /// </summary>
        /// <param name="classifier">The classifier generator</param>
        /// <param name="targetState">The target state of the shift operation</param>
        /// <param name="callTarget">The method representing the target state</param>
        /// <param name="stateParam">An Expression representing the parser state object</param>
        /// <param name="depthParam">An Expression representing the parse depth</param>
        /// <param name="stackValueParams">Expressions representing the values on the stack</param>
        /// <param name="term">The terminal which needs to be matched in order for this shift to take place</param>
        /// <param name="allTerminals">All terminals in the grammar</param>
        /// <param name="parserTypeBuilder">The type builder for the parser class</param>
        /// <returns>An expression that represents an LR(1) "shift" operation</returns>
        Expression GenerateShiftWithoutValue(TerminalClassifierSession<TChar> classifier, LRParseState<TChar> targetState, MethodInfo callTarget, Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            Terminal<TChar> term, IDictionary<Terminal<TChar>, int> allTerminals, TypeBuilder parserTypeBuilder)
        {
            // Generate code to represent a "shift" action where the terminal does not have an associated value.  The value stack remains unchanged.
            var readTerminal = GetReadTerminal(stateParam, classifier, allTerminals, targetState, parserTypeBuilder);
            var result = Expression.Call(callTarget, GetOutgoingParamList(stateParam, depthParam, stackValueParams, null));
            return Expression.Block(readTerminal, result);
        }

        /// <summary>
        /// Create an expression that represents an LR(1) "shift" operation.  Passes throughy to GenerateShiftWithValue or GenerateShiftWithoutValue depending on whether the terminal has an associated value.
        /// </summary>
        /// <param name="classifier">The classifier generator</param>
        /// <param name="targetState">The target state of the shift operation</param>
        /// <param name="callTarget">The method representing the target state</param>
        /// <param name="stateParam">An Expression representing the parser state object</param>
        /// <param name="depthParam">An Expression representing the parse depth</param>
        /// <param name="stackValueParams">Expressions representing the values on the stack</param>
        /// <param name="term">The terminal which needs to be matched in order for this shift to take place</param>
        /// <param name="allTerminals">All terminals in the grammar</param>
        /// <param name="parserTypeBuilder">The type builder for the parser class</param>
        /// <returns>An expression that represents an LR(1) "shift" operation</returns>
        Expression GenerateShift(TerminalClassifierSession<TChar> classifier, LRParseState<TChar> targetState, MethodInfo callTarget, Expression stateParam, Expression depthParam, ParameterExpression[] stackValueParams,
            Terminal<TChar> term, IDictionary<Terminal<TChar>, int> allTerminals, TypeBuilder parserTypeBuilder)
        {
            // Generate code to represent a "shift" action
            bool hasValue = term.ValueType != null && term.ValueType != typeof(void);

            return hasValue ? GenerateShiftWithValue(classifier, targetState, callTarget, stateParam, depthParam, stackValueParams,
                term, allTerminals, parserTypeBuilder)
                : GenerateShiftWithoutValue(classifier, targetState, callTarget, stateParam, depthParam, stackValueParams,
                term, allTerminals, parserTypeBuilder);
        }

        /// <summary>
        /// Create an Expression representing an LR(1) "reduce" operation
        /// </summary>
        /// <param name="stateParam">An Expression representing the parser state object</param>
        /// <param name="depthParam">An Expression representing the parse depth</param>
        /// <param name="stackValueParams">Expressions representing the values on the value stack</param>
        /// <param name="rule">The rule being matched by this reduction</param>
        /// <param name="allNonTerminals">All nonterminals in the grammar</param>
        /// <returns></returns>
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

                // If the semantic action needs the parser state object, pass it in.
                // NOTE: The parser state object is only supported as the semantic action's first parameter.
                // Otherwise skip it.
                if (rule.Action.Parameters[0].Type == stateParam.Type)
                {
                    actionParams.Add(stateParam);
                    numStateParams = 1;
                }
                else
                    numStateParams = 0;

                // Pass the value parameters in, ordering them so that the last parameter comes from the top of the stack.
                // That way, the semantic action for A->BC gets called with B's value followed by C's value.
                // TODO: Check the types and numbers of the values being passed in.
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
                // If we've gotten here, the left hand symbol needs a value but no semantic action was provided to create it.
                // We support some default symbol value building operations here.
                var destType = rule.LeftHandSide.ValueType;

                var inputSymbols = (from x in rule.RightHandSide where (x.ValueType != null && x.ValueType != typeof(void)) select x).ToList();
                Expression ntValue = null;
                if (inputSymbols.Count() == 1)
                {
                    if (destType.IsAssignableFrom(inputSymbols[0].ValueType))
                    {
                        // Types match, just pass the value through.
                        ntValue = stackValueParams[0];
                    }
                    else if (destType.IsAssignableFrom(typeof(List<>).MakeGenericType(inputSymbols[0].ValueType)))
                    {
                        // Left hand type is a list where the list elements are of the right hand type.  Create a list with one element
                        // and use that as the left hand symbol's value.
                        var newlist = Expression.Parameter(typeof(List<>).MakeGenericType(inputSymbols[0].ValueType));
                        ntValue = Expression.Block(
                            new[] { newlist },
                            new Expression[] {
                                Expression.Assign(newlist, Expression.New(typeof(List<>).MakeGenericType(inputSymbols[0].ValueType))),
                                Expression.Call(newlist, "Add", null, Expression.Convert(stackValueParams[0], inputSymbols[0].ValueType)),
                                newlist});
                    }
                    else
                    {
                        throw new ApplicationException("Right hand symbol's value cannot be cast to the left hand symbol's type");
                    }
                }
                else if (inputSymbols.Count() == 2)
                {
                    var p0 = Expression.Convert(stackValueParams[0], inputSymbols[0].ValueType);
                    var p1 = Expression.Convert(stackValueParams[1], inputSymbols[1].ValueType);
                    // If the left hand symbol's value type is a list, see if one of the right hand symbol types is the same type,
                    // and the other right hand symbol type is the element type of that list type.
                    // If so, we'll append the element to the incoming list and use the list as the left hand symbol's value.
                    if (destType == p1.Type && typeof(IList<>).MakeGenericType(p0.Type).IsAssignableFrom(destType))
                    {
                        // Switch them so we only have to handle one case.
                        var temp = p0;
                        p0 = p1;
                        p1 = temp;
                    }
                    if (destType == p0.Type && typeof(ICollection<>).MakeGenericType(p1.Type).IsAssignableFrom(destType))
                    {
                        // dest & p0 are a list, p1 is an element of that list.
                        ntValue = Expression.Block(
                            new Expression[] {
                                Expression.Call(Expression.Convert(p0, typeof(ICollection<>).MakeGenericType(p1.Type)), "Add", null, p1),
                                p0});
                    }
                }

                if (ntValue == null)
                {
                    // None of the cases above apply.
                    // Try to find an input symbol value that can be passed through.
                    int i = 0;
                    foreach (var x in inputSymbols)
                    {
                        if (destType.IsAssignableFrom(x.ValueType))
                        {
                            ntValue = Expression.Convert(stackValueParams[i], destType);
                            break;
                        }
                        ++i;
                    }
                    
                    if (ntValue == null)
                    {
                        // We're completely out of luck, the developer really needs to put a semantic action on this rule.
                        throw new ApplicationException("No symbol values found on the right side that can be case to the left hand symbol's type");
                    }
                }

                // Set whatever value we've derived above as the left hand symbol's value.
                statements.Add(Expression.Invoke(this.SetNonTerminalValueExpr(rule.LeftHandSide.ValueType), stateParam,
                    Expression.Convert(ntValue, rule.LeftHandSide.ValueType)));
            }
            
            // The execution stack needs to have one frame popped for each symbol on the right hand side of the rule.
            int depthChange = rule.RightHandSide.Count;
            statements.Add(Expression.Subtract(depthParam, Expression.Constant(depthChange)));

            return Expression.Block(statements);
        }

        /// <summary>
        /// Populate a MethodBuilder with code to implement a single LR(1) state.
        /// </summary>
        /// <param name="stateMethodBuilder">The MethodBuilder to populate.  Its body should be empty when this method is called.</param>
        /// <param name="numValueParams">The number of stack value parameters for this state method</param>
        /// <param name="state">The LR(1) state that this method will implement</param>
        /// <param name="callTargets">A map of LR(1) states to implementing methods.  Transitions to other states will be represented by calls to the corresponding methods</param>
        /// <param name="classifier">The token classifier generator</param>
        /// <param name="allTerminals">All terminals in the grammar</param>
        /// <param name="allNonTerminals">All non-terminals in the grammar</param>
        /// <param name="parserTypeBuilder"></param>
        void GenerateState(MethodBuilder stateMethodBuilder, int numValueParams, LRParseState<TChar> state,
            IDictionary<LRParseState<TChar>, MethodInfo> callTargets,
            TerminalClassifierSession<TChar> classifier,
            IDictionary<Terminal<TChar>, int> allTerminals,
            IDictionary<NonTerminal, int> allNonTerminals,
            TypeBuilder parserTypeBuilder)
        {
            // A state method takes as parameters the parser state, current parser stack depth, and the value stack, and returns the stack depth
            // that the parsing stack frame should have after this state's reductions and gotos are done.
            // The body of the state method goes like this:
            //
            // StateMethod(parserState, depth, stack0, stack1, ..., stackN)
            // {
            //    switch(NextTerminal)
            //    {
            //       // Cases come from Action table.
            //       case Terminal1:
            //         // Terminal has a value
            //         readNextTerminal();
            //         newDepth = OtherStateMethod(parserState, depth+1, CurrentTerminalValue, stack0, ..., stackN-1);
            //         break;
            //       case Terminal2:
            //         // Terminal does not have a value
            //         readNextTerminal();
            //         newDepth = OtherStateMethod(parserState, depth+1, stack0, stack1, ..., stackN);
            //         break;
            //       case Terminal3:
            //       case Terminal4:
            //         // Reduce on rule R
            //         CurrentNonTerminalValue = R.SemanticAction(stack1, stack0);
            //         newDepth = depth - R.NumSymbols
            //         break;
            //       ...
            //       default:
            //         // We've got a big fat parsing error here.
            //         // A new depth of -1 will return us all the way back to the beginning and signal an error.
            // TODO: Implement real error reporting and recovery.
            //         return -1;
            //    }
            //    // If there aren't any entries in the Goto table for this state,
            //    // the entire loop and everything in it goes away, and shifts can beome tail calls.
            // TODO: Verify that shifts actually become tail calls in that case.
            //    while(newDepth == depth)
            //    {
            //       // Cases come from Goto table
            //       switch(CurrentNonTerminal)
            //       {
            //         case NonTerminal1:
            //           // NonTerminal has a value
            //           newDepth = OtherStateMethod(parserState, depth+1, CurrentNonTerminalValue, stack0, ..., stackN-1);
            //           break;
            //         case NonTerminal2:
            //           // NonTerminal does not have a value
            //           newDepth = OtherStateMethod(parserState, depth+1, stack0, stack1, ..., stackN);
            //           break;
            //         ...
            //       }
            //    }
            //    return newDepth
            //  }
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

        /// <summary>
        /// Generates a parser for the parse table and includes it in a newly created RunAndCollect assembly.
        /// </summary>
        /// <param name="prefix">A unique prefix to be added to a newly-created internal support class</param>
        /// <param name="parseTable">The LR(1) parse table</param>
        /// <param name="classifier">The classifier generator</param>
        /// <returns></returns>
        public LambdaExpression Generate(string prefix, LRParseTable<TChar> parseTable, TerminalClassifierSession<TChar> classifier)
        {
            var assmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("GeneratedParser"), AssemblyBuilderAccess.RunAndCollect);
            var modBuilder = assmBuilder.DefineDynamicModule("GeneratedParser.dll", _includeSymbols);
            var typeBuilder = modBuilder.DefineType(prefix + "ExpressionParser", TypeAttributes.Public);
            var result = Generate(prefix, modBuilder, typeBuilder, parseTable, classifier);
            typeBuilder.CreateType();
            return result;
        }

        /// <summary>
        /// Generates a parser for the parse table and includes it in the type constructed by parserTypeBuilder.
        /// </summary>
        /// <param name="prefix">A unique prefix to be added to a newly-created internal support class</param>
        /// <param name="parserModuleBuilder">The builder for the module that the parser will live in</param>
        /// <param name="parserTypeBuilder">The builder for the class that the parser will live in</param>
        /// <param name="parseTable">The LR(1) parse table</param>
        /// <param name="classifier">The classifier generator</param>
        /// <returns>A LambdaExpression for calling the start state of the parser.</returns>
        public LambdaExpression Generate(string prefix, ModuleBuilder parserModuleBuilder, TypeBuilder parserTypeBuilder,
            LRParseTable<TChar> parseTable,
            TerminalClassifierSession<TChar> classifier)
        {
            // Find out the maximum number of values that need to be passed to any nonterminal rule action
            // Each state method will need that number of stack value parameters.
            // TODO: Some state methods might be able to get by with fewer parameters, and we can sometimes determine what types the stack parameters should be.
            int numValueParams = parseTable.Rules.Where(rule => rule.Action != null).Aggregate(0, (current, rule) => Math.Max(current, rule.Action.Parameters.Count));

            classifier = classifier.CreateWithNewResultType(typeof(int));

            // Set up the method builders that we'll populate with lambdas.  They have to call each other mutually recursively, so they
            // all need to exist before we actually populate any of them.
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
            // and create a MethodBuilder for the body of each state method.  We won't actually put any code into
            // any state methods until all the MethodBuilders exist and all terminals and nonterminals have been processed
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
                var methodBuilderWrap = new MethodBuilderWrap(parserTypeBuilder, "State" + i, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), stateParameterTypes);

                var methodBuilder = methodBuilderWrap.GetMethodBuilder();

                ++i;
                stateMethodBuilders[state] = methodBuilder;
                stateMethodCallTargets[state] = methodBuilderWrap;

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

            // Build a passthrough method for the start method so we can have a lambda that calls it compiled
            // into a delegate or compiled into the parser type as needed.
            // NOTE: The deal is, you can use a MethodBuilderWrap in a lambda that gets compiled into a MethodBuilder,
            // but not in a lambda that gets compiled into a delegate.
            
            var passthruTypeBuilder = parserModuleBuilder.DefineType(prefix + "-PassthruCalls", TypeAttributes.Public);
            var startMethodPassthruBuilder = passthruTypeBuilder.DefineMethod("CallStartState", MethodAttributes.Public | MethodAttributes.Static,
                typeof(int), stateParameterTypes);
            _expressionHelper.PopulatePassthru(startMethodPassthruBuilder, stateMethodBuilders[parseTable.States.First()], stateParameterTypes.Length);
            var passthruType = passthruTypeBuilder.CreateType();
            var startMethod = passthruType.GetMethod("CallStartState", BindingFlags.Public | BindingFlags.Static);

            // Now we can start generating code for each state method.
            foreach (var state in parseTable.States)
            {
                var methodBuilder = stateMethodBuilders[state];
                GenerateState(methodBuilder, numValueParams, state, stateMethodCallTargets, classifier, allTerminals, allNonTerminals, parserTypeBuilder);
            }

            // Now to build the lambda that kicks off the entire parsing process.
            // It needs to read the first terminal, and then call the start state.
            //var startMethod = stateMethodCallTargets[parseTable.States.First()];

            // Read the first terminal
            var shift = GetReadTerminal(expParserState, classifier, allTerminals, parseTable.States.First(), null);
            // Call the state method for the start state.  Start with a depth of one.  Then depth 0 or -1 should cause the start state method to
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
