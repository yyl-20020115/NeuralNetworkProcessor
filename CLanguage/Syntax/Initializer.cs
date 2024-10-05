using System.Collections.Generic;

namespace CLanguage.Syntax;

public abstract class Initializer
{
    public InitializerDesignation? Designation { get; set; }
}

public class ExpressionInitializer : Initializer
{
    public Expression Expression { get; private set; }

    public ExpressionInitializer (Expression expr) => Expression = expr;

    public override string? ToString () => Expression.ToString ();
}

public class StructuredInitializer : Initializer
{
    public List<Initializer> Initializers { get; private set; }

    public StructuredInitializer ()
    {
        Initializers = [];
    }

    public void Add (Initializer init)
    {
        Initializers.Add (init);
    }
}

public class InitializerDesignation
{
    public List<InitializerDesignator> Designators { get; private set; }

    public InitializerDesignation (List<InitializerDesignator> des)
    {
        Designators = [.. des];
    }
}

public class InitializerDesignator
{
}
