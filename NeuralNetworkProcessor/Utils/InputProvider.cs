using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Utils
{
    public delegate IEnumerable<int> Input();
    public static class InputProvider
    {
        public static Input CreateInput(string text)
            => () => text.EnumerateRunes().Select(r => r.Value);

        public static Input CreateInput(TextReader reader)
        {
            return Input;
            IEnumerable<int> Input()
            {
                using var _reader = reader;
                while ((_reader.Read() is int r) && r != -1)
                {
                    if (char.IsHighSurrogate((char)r) && (_reader.Read() is int s) && s != -1 &&
                        char.IsSurrogatePair((char)r, (char)s))
                    {
                        yield return char.ConvertToUtf32((char)r, (char)s);
                    }
                    else
                    {
                        yield return (char)r;
                    }
                }
            }
        }
    }
}
