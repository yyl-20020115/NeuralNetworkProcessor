using System;

namespace CLanguage.Syntax;

public readonly struct Location (Document document, int index, int line, int column) : IEquatable<Location>
{
    public bool IsNull => Document == null;
    public readonly Document Document = document;
    public readonly int Index = index;
    public readonly int Line = line;
    public readonly int Column = column;

    public override string ToString () => IsNull ? "?(?,?)" : $"{Document}({Line},{Column})";

    public static Location operator + (Location location, int columnOffset) =>
        new(location.Document, location.Index + columnOffset, location.Line, location.Column + columnOffset);

    public static readonly Location Null = new Location ();

    public static bool operator == (Location x, Location y) => x.Line == y.Line && x.Column == y.Column && x.Document?.Path == y.Document?.Path;
    public static bool operator != (Location x, Location y) => x.Line != y.Line || x.Column != y.Column || x.Document?.Path != y.Document?.Path;

    public override bool Equals (object? obj) => obj is Location && Equals ((Location)obj);
    public bool Equals (Location y) => Line == y.Line && Column == y.Column && Document.Path == y.Document.Path;

    public override int GetHashCode ()
    {
        var hashCode = 1439312346;
        hashCode = hashCode * -1521134295 + (Document != null ? Document.Path.GetHashCode () : 0);
        hashCode = hashCode * -1521134295 + Line.GetHashCode ();
        hashCode = hashCode * -1521134295 + Column.GetHashCode ();
        return hashCode;
    }
}
