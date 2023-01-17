using NeuralNetworkProcessor.Util;
using NeuralNetworkProcessor.ZRF;
using Utilities;

namespace NeuralNetworkProcessor.NT;

public enum GNodeType : uint
{
    Normal = 0,
    StartJoint = 1,
    EndJoint = 2,
    Terminal = 3,
    Transport = 4,
}
public record class GNode(int Ch = 0,string Name = "", Phrase Phrase = null, Description Description = null, Definition Definition = null, GNodeType Type= GNodeType.Normal, UnicodeClass UnicodeClass = UnicodeClass.Unknown,GNetwork Network = null, CharRange? CharRange = null)
{
    public static readonly GNode EOF = new(-1, Name: "\uffff", Type: GNodeType.Terminal);
    public GNodeType Type { get; set; } = Type;
    public CharRange? CharRange { get; set; } = CharRange;
    public bool Match(int ch) => 
        (this.Type == GNodeType.Terminal &&(this.UnicodeClass != UnicodeClass.Unknown
            ? CharRange?.InRange(ch)
              ?? this.Ch == ch
                : this.UnicodeClass ==
                    (UnicodeClass)char.GetUnicodeCategory(UnicodeClassTools.ToText(ch), 0)));

    public override string ToString() => !string.IsNullOrEmpty(this.Name) ? this.Name :
        UnicodeClass != UnicodeClass.Unknown 
        ? UnicodeClass.ToString() : this.CharRange?.ToString()
        ?? UnicodeClassTools.ToText(this.Ch);
}
