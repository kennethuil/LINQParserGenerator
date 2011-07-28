using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class LR1ItemSetCollection<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        Canonicalizer<ISet<LR1Item<TChar>>> _itemSets;
        Dictionary<ISet<LR1Item<TChar>>, IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>>> _gotoMap
            = new Dictionary<ISet<LR1Item<TChar>>, IDictionary<GrammarSymbol, ISet<LR1Item<TChar>>>>();
        IDictionary<GrammarSymbol, ISet<GrammarSymbol>> _firstSets;
        Grammar<TChar> _grammar;

        public LR1ItemSetCollection(Grammar<TChar> grammar)
        {
            _firstSets = GetFirstSets(grammar);
            _grammar = grammar;

            var startItem = new LR1Item<TChar> { Rule = grammar.StartRule, Lookahead = Eof<TChar>.Instance, DotPosition = 0 };
            var startClosure = Closure(new HashSet<LR1Item<TChar>> { startItem });
            _itemSets = new Canonicalizer<ISet<LR1Item<TChar>>>(new[] { startClosure },
                                                            new SetComparer<LR1Item<TChar>>());
        }

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

        public ISet<GrammarSymbol> First(IEnumerable<GrammarSymbol> symbolString)
        {
            var result = new HashSet<GrammarSymbol>();
            foreach (var symbol in symbolString)
            {
                var added = GetSet(_firstSets, symbol);
                foreach (var s in added)
                {
                    result.Add(s);
                }
                if (!added.Contains(Epsilon.Instance))
                    break;
            }
            return result;
        }

        public ISet<LR1Item<TChar>> Closure(ISet<LR1Item<TChar>> iset)
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
                        from x in _grammar.Rules where x.LeftHandSide == B select x)
                    {
                        foreach (var b in
                            from x in First(beta.Concat(new GrammarSymbol[] { a }))
                            where x is Terminal<TChar>
                            select (Terminal<TChar>)x)
                        {
                            added |= iset.Add(new LR1Item<TChar>
                            {
                                DotPosition = 0,
                                Lookahead = b,
                                Rule = new GrammarRule { LeftHandSide = B, RightHandSide = rule.RightHandSide, Action = rule.Action }
                            });
                        }
                    }
                }
            } while (added);
            return iset;
        }

        public ISet<LR1Item<TChar>> Goto(ISet<LR1Item<TChar>> I, GrammarSymbol X)
        {
            var J = new HashSet<LR1Item<TChar>>();

            foreach (var item in
                from i in I where i.SymbolAfterDot == X select i)
            {
                J.Add(item.MoveDotRightOne());
            }
            return Closure(J);
        }

        public bool Add(ISet<LR1Item<TChar>> item)
        {
            var canonical = _itemSets.GetInstance(item);
            bool isNew = Object.ReferenceEquals(item, canonical);


            return isNew;
        }
    }
}
