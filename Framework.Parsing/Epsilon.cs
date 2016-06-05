using System;


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
