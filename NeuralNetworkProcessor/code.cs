using NeuralNetworkProcessor.Core;
namespace nn.samples{
    public partial record Node;
    public partial record WhiteSpace(
        [Pattern(
            "+'&#x0009;'",
            "+'&#x000B;'",
            "+'&#x000C;'",
            "+'&#x0020;'",
            "+'&#x00A0;'",
            "+'&#+Zs;'",
            IsList = true)]
            string _
    ) : Node;

    public partial record LineTerminator(
        [Pattern(
            "+'&#x000A;'",
            "+'&#x000D;'",
            "+'&#x2028;'",
            "+'&#x2029;'",
            "+'&#x00A0;'",
            "+'&#x0085;'",
            IsList = true)]
            string _
    ) : Node;

    public partial record Space(
        [Pattern("")]WhiteSpace _0,
        [Pattern("")]LineTerminator _1
    ) : Node;

    public partial record _(
        [Pattern("")]Space _0,
        [Pattern("", "")](_, Space) _1
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
            IsList = true)]
            string _
    ) : Node;

    public partial record Integer(
        [Pattern("")]Digit _0,
        [Pattern("", "")](Integer, Digit) _1
    ) : Node;

    public partial record LParen(
        [Pattern(null, ")")](_, string) _0
    ) : Node;

    public partial record RParen(
        [Pattern(null, ")")](_, string) _0
    ) : Node;

    public partial record Factor(
        [Pattern("", null, "", "")](LParen, _, Expression, RParen) _0,
        [Pattern("")]Integer _1
    ) : Node;

    public partial record Mul(
        [Pattern(null, "*")](_, string) _0
    ) : Node;

    public partial record Div(
        [Pattern(null, "/")](_, string) _0
    ) : Node;

    public partial record Add(
        [Pattern(null, "+")](_, string) _0
    ) : Node;

    public partial record Sub(
        [Pattern(null, "-")](_, string) _0
    ) : Node;

    public partial record Term(
        [Pattern("", "", null, "")](Term, Mul, _, Factor) _0,
        [Pattern("", "", null, "")](Term, Div, _, Factor) _1,
        [Pattern("")]Factor _2
    ) : Node;

    public partial record Expression(
        [Pattern("", "", null, "")](Expression, Add, _, Term) _0,
        [Pattern("", "", null, "")](Expression, Sub, _, Term) _1,
        [Pattern("")]Term _2
    ) : Node;

    public partial record Top(
        [Pattern(null)]_ _0,
        [Pattern(null, "", null)](_, Expression, _) _1
    ) : Node;

}