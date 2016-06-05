using System;

namespace Framework.Parsing
{
    [Serializable]
    public class GrammarSymbol
    {
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
        [NonSerialized]
        Type _valueType;
        public virtual Type ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }


    }
}
