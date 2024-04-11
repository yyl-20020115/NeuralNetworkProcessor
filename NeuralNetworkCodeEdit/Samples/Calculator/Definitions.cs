using NeuralNetworkProcessor.Core;

namespace NeuralNetworkCodeEdit.Calculator;

public partial record WhiteSpace(
    [Pattern(
        "&#x0009;",
        "&#x000B;",
        "&#x000C;",
        "&#x0020;",
        "&#x00A0;",
        "&#+Zs;",
        AsPatterns = true)]
        string _ = default
    ) : Node;
public partial record LineTerminator(
    [Pattern(
        "&#x000A;",
        "&#x000D;",
        "&#x2028;",
        "&#x2029;",
        "&#x00A0;",
        "&#x0085;",
        AsPatterns = true
        )]string _ = default
    ) : Node;
public partial record Space(
    [Pattern] WhiteSpace WhiteSpace = default,
    [Pattern] LineTerminator LineTerminator = default
    ) : Node;
public partial record _(
    [Pattern] Space Space = default,
    [Pattern] (_, Space) __ = default
    ) : Node;
public partial record Digit(
    [Pattern(
        "0",
        "1",
        "2",
        "3",
        "4",
        "5",
        "6",
        "7",
        "8",
        "9",
        AsPatterns = true
    )]
    string _ = default
    ) : Node;
public partial record Integer(
    [Pattern] Digit Digit = default,
    [Pattern] (Integer, Digit) Integer_Digit = default
    ) : Node;
public partial record LParen(
    [Pattern(null, "(")] (_, string) _ = default
    ) : Node;
public partial record RParen(
    [Pattern(null, ")")] (_, string) _ = default
    ) : Node;
public partial record Default(
    [Pattern("default")] string _ = default
    ) : Node;
public partial record Mul(
    [Pattern(null, "*")] (_, string) _ = default
    ) : Node;
public partial record Div(
    [Pattern(null, "/")] (_, string) _ = default
    ) : Node;
public partial record Add(
    [Pattern(null, "+")] (_, string) _ = default
    ) : Node;
public partial record Sub(
    [Pattern(null, "-")] (_, string) _ = default
    ) : Node;
public partial record Term(
    [Pattern(2)] (Term Term, Mul Mul, _ _, Factor Factor) Term_Mul_Factor = default,
    [Pattern(2)] (Term Term, Div Div, _ _, Factor Factor) Term_Div_Factor = default,
    [Pattern] Factor Factor = default
    ) : Node;
public partial record Expression(
    [Pattern(2)] (Expression Exp, Add Add, _ _, Term Term) Exp_Add_Term = default,
    [Pattern(2)] (Expression Exp, Sub Sub, _ _, Term Term) Exp_Sub_Term = default,
    [Pattern] Term Term = default
    ) : Node;
public partial record Factor(
    [Pattern(1)] (LParen, _, Expression Exp, RParen) LParen_Expression_RParen = default,
    [Pattern] Integer Integer = default,
    [Pattern] Default DefaultValue = default
    ) : Node;
public partial record Top(
    [Pattern(0)] _ _ = default,
    [Pattern(0, 2)] (_, Expression Exp, _) Expression = default
    ) : Node;
