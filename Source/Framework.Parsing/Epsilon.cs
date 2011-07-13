using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class Epsilon : GrammarSymbol
    {
        private Epsilon()
        {
            this.Name = "<epsilon>";
        }

        public static readonly Epsilon Instance = new Epsilon();
    }
}
