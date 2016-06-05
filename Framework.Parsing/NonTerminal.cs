using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public class NonTerminal : GrammarSymbol
    {
        public NonTerminal()
        {
        }

        public NonTerminal(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    public class NonTerminal<TValue> : NonTerminal
    {
        public override Type ValueType
        {
            get
            {
                return typeof(TValue);
            }
            set
            {
                if (value != typeof(TValue))
                    throw new NotSupportedException();
            }
        }

        public NonTerminal()
            : base()
        {
        }

        public NonTerminal(string name)
            : base(name)
        {
        }
    }
}
