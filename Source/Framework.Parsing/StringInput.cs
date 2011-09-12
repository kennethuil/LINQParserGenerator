using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class StringInput
    {
        String _input;
        int _pos;
        int? _markPos;

        public StringInput(string input)
        {
            _input = input;
        }

        public bool HasCurrentChar()
        {
            return _pos < _input.Length;
        }

        public char CurrentChar()
        {
            return _input[_pos];
        }

        public void MoveNextChar()
        {
            _pos++;
        }

        public void MarkPos()
        {
            _markPos = _pos;
        }

        public void UnmarkPos()
        {
            _markPos = null;
        }

        public string GetFromMarkedPos()
        {
            var text = _input.Substring(_markPos.Value, _pos - _markPos.Value);
            return text;
        }

        public int GetPos()
        {
            return _pos;
        }
    }
}
