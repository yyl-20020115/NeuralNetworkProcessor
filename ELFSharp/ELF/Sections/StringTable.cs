using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections;
public sealed class StringTable<T> : Section<T>, IStringTable where T : struct
{
    private Dictionary<long, string> stringCache;
    private byte[] stringBlob;
    private bool cachePopulated;

    internal StringTable(SectionHeader header, SimpleEndianessAwareReader reader) 
        : base(header, reader)
    {
        stringCache = new (){ [0] = string.Empty };
        stringBlob = ReadStringData();
    }

    public IEnumerable<string> Strings 
        => !cachePopulated ? PrepopulateCache().Values : stringCache.Values;

    public string this[long index] => stringCache.TryGetValue(index, out string result) 
        ? result : HandleUnexpectedIndex(index);

    private string HandleUnexpectedIndex(long index)
    {
        var stringStart = (int)index;
        for (var i = stringStart; i < stringBlob.Length; ++i)
        {
            if (stringBlob[i] == 0)
            {
                var str = Encoding.UTF8.GetString(stringBlob, stringStart, i - stringStart);
                stringCache.Add(stringStart, str);
                return str;
            }
        }
        throw new IndexOutOfRangeException();
    }

    private Dictionary<long,string> PrepopulateCache()
    {
        cachePopulated = true;
        var stringStart = 1;
        for (var i = 1; i < stringBlob.Length; ++i)
        {
            if (stringBlob[i] == 0)
            {
                if (!stringCache.ContainsKey(stringStart))
                {
                    stringCache.Add(stringStart, Encoding.UTF8.GetString(stringBlob, stringStart, i - stringStart));
                }
                stringStart = i + 1;
            }
        }
        return stringCache;
    }

    private byte[] ReadStringData()
    {
        SeekToSectionBeginning();
        var blob = Reader.ReadBytes((int)Header.Size);
        Debug.Assert(blob.Length == 0 || (blob[0] == 0 && blob[blob.Length - 1] == 0), "First and last bytes must be the null character (except for empty string tables)");
        return blob;
    }
}
