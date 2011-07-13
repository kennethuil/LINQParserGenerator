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

        /*
        void GenerateState(MethodBuilder stateMethodBuilder, LRParseState<TChar> state,
            IDictionary<LRParseState<TChar>, MethodInfo> callTargets,
            TerminalClassifierSession<TChar> classifier,
            IDictionary<Terminal<TChar>, int> allTerminals,
            LambdaExpression getCurrentTerminal,
            LambdaExpression setCurrentTerminal)
        {
            // State function parameters.
            ParameterExpression expState = Expression.Parameter(classifier.StateType);
            ParameterExpression expDepth = Expression.Parameter(typeof(int));
            ParameterExpression expValueStack = Expression.Parameter(typeof(List<object>));

            // Handle actions.
            List<SwitchCase> actionCases = new List<SwitchCase>();
            foreach (var actionEntry in state.Actions)
            {
                var terminal = actionEntry.Key;
                var actions = actionEntry.Value;
                // TODO: If you see multiple actions, put in some GLR goodness.
                var action = actions.FirstOrDefault();
                if (action == null)
                    continue;

                if (action is ShiftAction<TChar>)
                {
                    var shift = (ShiftAction<TChar>)action;
                    var nextState = shift.TargetState;
                    var nextPossibleTerminals = GetPossibleTerminals(allTerminals, nextState);
                    var readTerminalLambda = classifier.Generate(nextPossibleTerminals);


                }
            }
        }

        public LambdaExpression Generate(string prefix, ModuleBuilder parserModuleBuilder, TypeBuilder parserTypeBuilder,
            LRParseTable<TChar> parseTable,
            TerminalClassifierSession<TChar> classifier,
            LambdaExpression getCurrentTerminal,
            LambdaExpression setCurrentTerminal)
        {
            classifier = classifier.CreateWithNewResultType(typeof(int));
            // Set up the method builders that we'll populate with lambdas.  They have to call each other mutually recursively, so
            // we have to create a separate type with passthrough calls back to the MethodBuilders, since lambda expression trees
            // can't refer to method builders.
            IDictionary<LRParseState<TChar>, MethodBuilder> stateMethodBuilders = new Dictionary<LRParseState<TChar>, MethodBuilder>();
            IDictionary<LRParseState<TChar>, MethodInfo> stateMethodCallTargets = new Dictionary<LRParseState<TChar>, MethodInfo>();
            int i = 0;
            IDictionary<Terminal<TChar>,int> allTerminals = new Dictionary<Terminal<TChar>,int>();

            int terminalNumber = 1;
            ParameterExpression expParserState = Expression.Parameter(classifier.StateType);
            classifier.EofHandlerIs(Expression.Lambda(Expression.Constant(0), true, expParserState));

            foreach (var state in parseTable.States)
            {
                var methodBuilder = parserTypeBuilder.DefineMethod("State" + i, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), new Type[] { classifier.StateType, typeof(int), typeof(List<object>) });
                ++i;
                stateMethodBuilders[state] = methodBuilder;
                int existing;

                foreach (var action in state.Actions)
                {
                    if (!allTerminals.TryGetValue(action.Key, out existing))
                    {
                        var term = action.Key;
                        allTerminals.Add(term, terminalNumber);
                        bool hasValue = term.ValueType != null && term.ValueType != typeof(void);

                        if (hasValue)
                        {
                            ParameterExpression expTerminalString;
                            Expression callAction;

                            if (term.Action != null)
                            {
                                if (term.Action.Parameters.Count() == 1)
                                {
                                    expTerminalString = Expression.Parameter(term.Action.Parameters[0].Type);
                                    callAction = Expression.Invoke(term.Action, expTerminalString);
                                }
                                else
                                {
                                    expTerminalString = Expression.Parameter(term.Action.Parameters[1].Type);
                                    callAction = Expression.Invoke(term.Action, expParserState, expTerminalString);
                                }

                            }
                            else
                            {
                                expTerminalString = Expression.Parameter(term.ValueType);
                                callAction = expTerminalString;
                            }
                            classifier.AddTerminalHandler(term,
                                Expression.Lambda(
                                    Expression.Block(
                                        callAction, 
                                        Expression.Constant(terminalNumber)), true, expParserState, expTerminalString));
                        }
                        else
                            classifier.AddTerminalHandler(term,
                                Expression.Lambda(
                                    Expression.Constant(terminalNumber), true, expParserState));
                        ++terminalNumber;
                    }

                }
            }

            var passthruTypeBuilder = parserModuleBuilder.DefineType(prefix + "-PassthruCalls", TypeAttributes.Public);

            i = 0;
            foreach (var state in parseTable.States)
            {
                var methodBuilder = passthruTypeBuilder.DefineMethod("CallState" + i, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), new[] { classifier.StateType, typeof(int), typeof(List<object>) });
                _expressionHelper.PopulatePassthru(methodBuilder, stateMethodBuilders[state], 3);
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

            // Now we can start creating lambdas and filling in the state method builders with them.
            foreach (var state in parseTable.States)
            {
                var methodBuilder = stateMethodBuilders[state];
                GenerateState(methodBuilder, state, stateMethodCallTargets, classifier, allTerminals, getCurrentTerminal, setCurrentTerminal);
            }
            var parserType = parserTypeBuilder.CreateType();
            var startMethod = parserType.GetMethod("State0");

            var shiftLambda = classifier.Generate(GetPossibleTerminals(allTerminals, parseTable.States.First()));
            
            var shift = Expression.Invoke(setCurrentTerminal, expParserState, Expression.Invoke(shiftLambda, expParserState));
            var start = Expression.Call(startMethod, expParserState, Expression.Constant(0), Expression.New(typeof(List<object>)));

            var body = Expression.Block(shift, start);

            // TODO: What do we return?
            return Expression.Lambda(start, true, expParserState);
        }
         */
    }

}
