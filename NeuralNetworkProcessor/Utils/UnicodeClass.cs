using System;
using System.Collections.Generic;
using System.Text;

namespace NeuralNetworkProcessor.Utils
{
    /// <summary>
    /// Same as UnicodeCategory plus Unknown and Any 
    /// </summary>
    public enum UnicodeClass : int
    {
        /// <summary>
        /// Unknown class is a default value for failure
        /// </summary>
        Unknown = -1,
        UppercaseLetter = 0,
        LowercaseLetter = 1,
        TitlecaseLetter = 2,
        ModifierLetter = 3,
        OtherLetter = 4,
        NonSpacingMark = 5,
        SpacingCombiningMark = 6,
        EnclosingMark = 7,
        DecimalDigitNumber = 8,
        LetterNumber = 9,
        OtherNumber = 10,
        SpaceSeparator = 11,
        LineSeparator = 12,
        ParagraphSeparator = 13,
        Control = 14,
        Format = 15,
        Surrogate = 16,
        PrivateUse = 17,
        ConnectorPunctuation = 18,
        DashPunctuation = 19,
        OpenPunctuation = 20,
        ClosePunctuation = 21,
        InitialQuotePunctuation = 22,
        FinalQuotePunctuation = 23,
        OtherPunctuation = 24,
        MathSymbol = 25,
        CurrencySymbol = 26,
        ModifierSymbol = 27,
        OtherSymbol = 28,
        OtherNotAssigned = 29,
        /// <summary>
        /// Any category/class, short name is "__"
        /// </summary>
        Any = 30
    }
    public static class UnicodeClassTools
    {
        public const int NULLChar = 0;
        public const int EOFChar = -1;

        public static readonly string[] ShortNames = new[]
        {
            "Lu",
            "Ll",
            "Lt",
            "Lm",
            "Lo",
            "Mn",
            "Mc",
            "Me",
            "Nd",
            "Nl",
            "No",
            "Zs",
            "Zl",
            "Zp",
            "Cc",
            "Cf",
            "Cs",
            "Co",
            "Pc",
            "Pd",
            "Ps",
            "Pe",
            "Pi",
            "Pf",
            "Po",
            "Sm",
            "Sc",
            "Sk",
            "So",
            "Cn",
            "__", //for all or any
        };
        public static UnicodeClass GetClassByShortName(string ShortName) => (UnicodeClass)System.Array.FindIndex(ShortNames,s => s == ShortName);
        public static UnicodeClass GetClassByLongName(string LongName) => System.Enum.TryParse(LongName, out UnicodeClass UC) ? UC : UnicodeClass.Unknown;
        public static string GetShortNameByClass(UnicodeClass unicodeClass)
            => unicodeClass > UnicodeClass.Unknown && unicodeClass <= UnicodeClass.Any
                ? ShortNames[(int)(unicodeClass)]
                : UnicodeClass.Unknown.ToString();
        public static string GetLongNameByClass(UnicodeClass unicodeClass) => unicodeClass.ToString();
        public const int UNICODE_PLANE00_END = 0x00ffff;
        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
        public const int UNICODE_PLANE01_START = 0x10000;
        // The end codepoint for Unicode plane 16.  This is the maximum code point value allowed for Unicode.
        // Plane 16 contains 0x100000 ~ 0x10ffff.
        public const int UNICODE_PLANE16_END = 0x10ffff;
        public const int HIGH_SURROGATE_START = 0x00d800;
        public const int LOW_SURROGATE_END = 0x00dfff;

        public const string DefaultInvalidUnicodeCharText = null;
        public static string ToText(int utf32, string InvalidUnicodeCharText = DefaultInvalidUnicodeCharText)
            => IsValidUnicode(utf32) ? char.ConvertFromUtf32(utf32) : InvalidUnicodeCharText;
        public static int FromText(string text)
            => text == null || text.Length == 0 ? NULLChar : char.ConvertToUtf32(text, 0);
        public static bool IsValidUnicode(int utf32)
            => !((utf32 < 0 || utf32 > UNICODE_PLANE16_END) || (utf32 >= HIGH_SURROGATE_START && utf32 <= LOW_SURROGATE_END));
        public static bool IsWideCharAt(string Text, int Index)
       => !string.IsNullOrEmpty(Text) && Index >= 0 && Index + 1 < Text.Length
           && char.IsHighSurrogate(Text, Index) && char.IsLowSurrogate(Text, Index + 1);
        public static int GetWideChar(string Text, int Index)
            => IsWideCharAt(Text, Index) ?
                char.ConvertToUtf32(Text, Index) : EOFChar;
        public static IEnumerable<int> NextWideChar(string text)
        {
            foreach (var r in text.EnumerateRunes())
                yield return r.Value;
        }
    }
}
