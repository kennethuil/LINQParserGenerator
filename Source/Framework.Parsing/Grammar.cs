using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public class Grammar<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        IEnumerable<GrammarRule> _rules;
        ISet<Terminal<TChar>> _terminals = new HashSet<Terminal<TChar>>();
        ISet<NonTerminal> _nonTerminals = new HashSet<NonTerminal>();
        ISet<GrammarSymbol> _symbols = new HashSet<GrammarSymbol>();

        protected void Init(IEnumerable<GrammarRule> rules)
        {
            // Create a new start rule pointing to the original start rule.
            NonTerminal sPrime = new NonTerminal { Name = "S'", ValueType = rules.First().LeftHandSide.ValueType };

            _rules = new GrammarRule[] {
                new GrammarRule {LeftHandSide = sPrime, RightHandSide = new GrammarSymbol[] {rules.First().LeftHandSide}}}
                .Concat(rules);

            // Populate the symbol set.
            foreach (var rule in _rules)
            {
                _nonTerminals.Add(rule.LeftHandSide);
                foreach (var symbol in rule.RightHandSide)
                {
                    if (symbol is Terminal<TChar>)
                    {
                        _terminals.Add((Terminal<TChar>)symbol);
                    }
                    else if (symbol is NonTerminal)
                    {
                        _nonTerminals.Add((NonTerminal)symbol);
                    }
                    _symbols.Add(symbol);
                }
                _terminals.Add(Eof<TChar>.Instance);
                _symbols.Add(Eof<TChar>.Instance);
            }
        }

        protected Grammar()
        {
        }

        public Grammar(IEnumerable<GrammarRule> rules)
        {
            Init(rules);
        }

        public IEnumerable<GrammarRule> Rules
        {
            get { return _rules; }
        }

        public GrammarRule StartRule
        {
            get { return _rules.First(); }
        }

        public ISet<Terminal<TChar>> Terminals
        {
            get { return _terminals; }
        }

        public ISet<NonTerminal> NonTerminals
        {
            get { return _nonTerminals; }
        }

        public ISet<GrammarSymbol> Symbols
        {
            get { return _symbols; }
        }
    }
}
