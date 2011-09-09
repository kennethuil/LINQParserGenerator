using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class LRParseTableBuilder
    {
        // Helpers to manipulate sets and things.
        bool Add(IDictionary<GrammarSymbol, ISet<GrammarSymbol>> symbolSetTable, GrammarSymbol owner, GrammarSymbol item)
        {
            ISet<GrammarSymbol> set;
            if (!symbolSetTable.TryGetValue(owner, out set))
            {
                set = new HashSet<GrammarSymbol>();
                symbolSetTable.Add(owner, set);
            }
            return set.Add(item);
        }

        ISet<GrammarSymbol> GetSet(IDictionary<GrammarSymbol, ISet<GrammarSymbol>> symbolSetTable, GrammarSymbol owner)
        {
            ISet<GrammarSymbol> set;
            if (!symbolSetTable.TryGetValue(owner, out set))
            {
                set = EmptySet<GrammarSymbol>.Instance;
            }
            return set;
        }

        bool Add(IDictionary<GrammarSymbol, ISet<GrammarSymbol>> symbolSetTable, GrammarSymbol owner, ISet<GrammarSymbol> items)
        {
            bool updated = false;
            foreach (var item in items)
            {
                updated |= Add(symbolSetTable, owner, item);
            }
            return updated;
        }

        // Compute FIRST sets for all symbols.
        public IDictionary<GrammarSymbol, ISet<GrammarSymbol>> GetFirstSets<TChar>(Grammar<TChar> grammar)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var firstSets = new Dictionary<GrammarSymbol, ISet<GrammarSymbol>>();

            // First pass.
            foreach (var t in grammar.Terminals)
            {
                Add(firstSets, t, t);
            }
            foreach (var rule in grammar.Rules)
            {
                if (rule.RightHandSide.Count == 1 && rule.RightHandSide.First() == Epsilon.Instance)
                {
                    Add(firstSets, rule.LeftHandSide, Epsilon.Instance);
                }
            }

            // Repeat until no more updates
            bool updated;

            do
            {
                updated = false;

                foreach (var rule in grammar.Rules)
                {
                    foreach (var symbol in rule.RightHandSide)
                    {
                        var added = GetSet(firstSets, symbol);
                        updated |= Add(firstSets, rule.LeftHandSide, added);
                        if (!added.Contains(Epsilon.Instance))
                            break;
                    }
                }
            } while (updated);

            return firstSets;
        }

        // Get FIRST set for a string of symbols
        public ISet<GrammarSymbol> First(IDictionary<GrammarSymbol, ISet<GrammarSymbol>> firstSets, IEnumerable<GrammarSymbol> symbolString)
        {
            var result = new HashSet<GrammarSymbol>();
            foreach (var symbol in symbolString)
            {
                var added = GetSet(firstSets, symbol);
                foreach (var s in added)
                {
                    result.Add(s);
                }
                if (!added.Contains(Epsilon.Instance))
                    break;
            }
            return result;
        }

        public ISet<LR1Item<TChar>> Closure<TChar>(Grammar<TChar> grammar, IDictionary<GrammarSymbol, ISet<GrammarSymbol>> firstSets,
            ISet<LR1Item<TChar>> iset)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // Repeat until no more added
            bool added;
            do
            {
                added = false;
                foreach (var item in
                    (from x in iset where x.SymbolAfterDot is NonTerminal select x).ToArray())
                {
                    var B = (NonTerminal)item.SymbolAfterDot;
                    var beta = item.SymbolsAfterDot.Skip(1);
                    var a = item.Lookahead;
                    foreach (var rule in
                        from x in grammar.Rules where x.LeftHandSide == B select x)
                    {
                        foreach (var b in 
                            from x in First(firstSets, beta.Concat(new GrammarSymbol[] { a })) where x is Terminal<TChar>
                                select (Terminal<TChar>)x)
                        {
                            added |= iset.Add(new LR1Item<TChar>{DotPosition = 0, Lookahead = b,
                                Rule = new GrammarRule{LeftHandSide = B, RightHandSide = rule.RightHandSide, Action = rule.Action}});
                        }
                    }
                }
            } while (added);
            return iset;
        }

        public ISet<LR1Item<TChar>> Goto<TChar>(Grammar<TChar> grammar, IDictionary<GrammarSymbol, ISet<GrammarSymbol>> firstSets,
            ISet<LR1Item<TChar>> I, GrammarSymbol X)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var J = new HashSet<LR1Item<TChar>>();

            foreach (var item in
                from i in I where i.SymbolAfterDot == X select i)
            {
                J.Add(item.MoveDotRightOne());
            }
            return Closure(grammar, firstSets, J);
        }

        void DumpItemSet<TChar>(ISet<LR1Item<TChar>> itemSet)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // TODO: Dependency-inject and use integrated logging.
            foreach (var item in itemSet)
            {
                Debug.WriteLine(item);
            }
            Debug.WriteLine("----------------------");
        }

        public LRParseTable<TChar> BuildParseTable<TChar>(Grammar<TChar> grammar)
            where TChar: IComparable<TChar>, IEquatable<TChar>
        {
            var gotoMap = new Dictionary<ISet<LR1Item<TChar>>, IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>>>();

            // Build the canonical set of LR1 Item sets.

            // Start with an item and an item set for the initial rule.
            Canonicalizer<ISet<LR1Item<TChar>>> C = GetCanonicalLR1Sets(grammar, gotoMap);

            LRParseTable<TChar> t = CreateParseTable(grammar, gotoMap, C);

            return t;
        }

        public LRParseTable<TChar> CreateParseTable<TChar>(Grammar<TChar> grammar, Dictionary<ISet<LR1Item<TChar>>, IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>>> gotoMap, Canonicalizer<ISet<LR1Item<TChar>>> C)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var stateMap = new Dictionary<ISet<LR1Item<TChar>>, LRParseState<TChar>>();
            LRParseTable<TChar> t = new LRParseTable<TChar> { Rules = grammar.Rules.ToList(), States = new List<LRParseState<TChar>>() };
            foreach (var I in C)
            {
                // Map states to item sets and add reduction actions.
                var state = new LRParseState<TChar>
                                {
                                    Actions = new Dictionary<Terminal<TChar>, ICollection<LRParseAction<TChar>>>(),
                                    Goto = new Dictionary<NonTerminal, LRParseState<TChar>>()
                                };
                stateMap[I] = state;
                t.States.Add(state);

                foreach(var item in
                    from x in I where x.SymbolAfterDot == null select x)
                {
                    var lookahead = item.Lookahead;
                    ICollection<LRParseAction<TChar>> action;
                    if (!state.Actions.TryGetValue(lookahead, out action))
                    {
                        action = new List<LRParseAction<TChar>>();
                        state.Actions.Add(lookahead, action);
                    }

                    if (item.Rule.LeftHandSide == grammar.StartRule.LeftHandSide)
                    {
                        action.Add(new AcceptAction<TChar>());
                    }
                    else
                    {
                        action.Add(new ReduceAction<TChar> { ReductionRule = item.Rule });
                    }
                }
            }

            // Add shift actions & goto entries.
            foreach (var entry in gotoMap)
            {
                var I = entry.Key;
                var state = stateMap[I];

                var symbolGotoMap = entry.Value;
                foreach (var symbolGoto in symbolGotoMap)
                {
                    var symbol = symbolGoto.Key;
                    var targetSet = symbolGoto.Value;
                    
                    if (symbol is Terminal<TChar>)
                    {
                        ICollection<LRParseAction<TChar>> action;
                        if (!state.Actions.TryGetValue(((Terminal<TChar>)symbol), out action))
                        {
                            action = new List<LRParseAction<TChar>>();
                            state.Actions.Add((Terminal<TChar>)symbol, action);
                        }
                        action.Add(new ShiftAction<TChar> { TargetState = stateMap[targetSet] });
                    }
                    else if (symbol is NonTerminal)
                    {
                        state.Goto[(NonTerminal)symbol] = stateMap[targetSet];
                    }
                }
            }
            return t;
        }

        public Canonicalizer<ISet<LR1Item<TChar>>> GetCanonicalLR1Sets<TChar>(Grammar<TChar> grammar, Dictionary<ISet<LR1Item<TChar>>, IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>>> gotoMap)
                        where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var firstSets = GetFirstSets(grammar);
            var startItem = new LR1Item<TChar> { Rule = grammar.StartRule, Lookahead = Eof<TChar>.Instance, DotPosition = 0 };
            var startClosure = Closure(grammar, firstSets, new HashSet<LR1Item<TChar>> { startItem });

            var C = new Canonicalizer<ISet<LR1Item<TChar>>>(new[] { startClosure },
                                                            new SetComparer<LR1Item<TChar>>());
            IEnumerable<ISet<LR1Item<TChar>>> newItems = C.ToArray();
            // Now add to it until nothing more can be added
            // We'll also build a "goto map" so we don't have to regenerate GOTO sets below.
            bool added;
            do
            {
                added = false;
                var addedItems = new List<ISet<LR1Item<TChar>>>();

                foreach (var I in newItems)
                {
                    foreach (var X in grammar.Symbols)
                    {
                        var gotoSet = Goto(grammar, firstSets, I, X);
                        if (gotoSet == null || gotoSet.Count == 0)
                            continue;

                        var canonical = C.GetInstance(gotoSet);
                        if (Object.ReferenceEquals(gotoSet, canonical))
                        {
                            added = true;
                            addedItems.Add(gotoSet);

                        }
                        IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>> symbolGotoMap;
                        if (!gotoMap.TryGetValue(I, out symbolGotoMap))
                        {
                            symbolGotoMap = new Dictionary<GrammarSymbol, ISet<LR1Item<TChar>>>();
                            gotoMap.Add(I, symbolGotoMap);
                        }
                        symbolGotoMap[X] = canonical;
                    }
                }
                newItems = addedItems;
                
            } while (added);
            
            // Now the canonical set of LR1 Item sets is complete.  Use it to build the parse table.

            // TODO: Dependency-inject and use integrated logging.
            /*
            foreach (var I in C)
            {
                DumpItemSet(I);
            }
             */
            return C;
             
        }
    }
}
