using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Utilities
{
    public delegate IEnumerable<(int,bool)> Input();
    public static class InputProvider
    {
        public static Input CreateInput(string text)
        {
            IEnumerable<(int,bool)> f()
            {
                var vs = text.EnumerateRunes().Select(r => r.Value).ToArray();
                for(int i = 0;i<vs.Length;i++)
                {
                    yield return (vs[i], i == vs.Length - 1);
                }
            };
            return f;
        }
        public static Input CreateInput(TextReader reader)
        {
            return Input;
            IEnumerable<(int,bool)> Input()
            {
                using var _reader = reader;
                while ((_reader.Read() is int r) && r != -1)
                    if (char.IsHighSurrogate((char)r) && (_reader.Read() is int s) && s != -1 &&
                        char.IsSurrogatePair((char)r, (char)s))
                        yield return (char.ConvertToUtf32((char)r, (char)s), _reader.Peek() == -1);
                    else
                        yield return ((char)r, _reader.Peek() == -1);
            }
        }
    }
}
