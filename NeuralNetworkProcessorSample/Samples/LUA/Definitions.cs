using NeuralNetworkProcessor.Core;

namespace NeuralNetworkProcessorSample.LUA
{
    public partial record Node;
    public partial record WhiteSpace(
        [Pattern(
            "+'&#x0009;'",
            "+'&#x000B;'",
            "+'&#x000C;'",
            "+'&#x0020;'",
            "+'&#x00A0;'",
            "+'&#+Zs;'",
            AsPatterns = true)]
            string _ = default
        ) : Node
        ;
    public partial record LineTerminator(
        [Pattern(
            "+'&#x000A;'",
            "+'&#x000D;'",
            "+'&#x2028;'",
            "+'&#x2029;'",
            "+'&#x00A0;'",
            "+'&#x0085;'",
            AsPatterns = true
            )]string _ = default
        ) : Node
        ;
    public partial record Space(
        [Pattern] WhiteSpace _0 = default,
        [Pattern] LineTerminator _1 = default
        ) : Node
        ;
    public partial record _(
        [Pattern] Space _0 = default,
        [Pattern] (_ _, Space Space) _1 = default
        ) : Node
        ;
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
        ) : Node
        ;
    public partial record Integer(
        [Pattern] Digit _0 = default,
        [Pattern] (Integer Integer, Digit Digit) _1 = default
        ) : Node
        ;
    public partial record Double(
        //TODO:
        ) : Node
        ;
    public partial record Numeral(
        [Pattern] Integer _0 = default,
        [Pattern] Double _2 = default
        ) : Node
        ;
    public partial record LiteralString(
        //TODO:
        ) : Node
        ;
    public partial record UnaryOperation(
        [Pattern(
            "-",
            "not",
            "#",
            "~",
            AsPatterns = true
        )]
        string _ = default
        ) : Node
        ;
    public partial record BinaryOperation(
        [Pattern(
            "+",
            "-",
            "*",
            "/",
            "//",
            "^",
            "%",
            "&",
            "~",
            "|",
            ">>",
            "<<",
            "..",
            "<",
            "<=",
            ">",
            ">=",
            "==",
            "~=",
            "and",
            "or",
            AsPatterns = true
        )]
        string _ = default
        ) : Node
        ;    
    public abstract record Statement(
        ) : Node
        ;
    public abstract record Expression(
        ) : Node
        ;
    public partial record Expressions(
        [Pattern]Expression Expression = default,
        [Pattern](Expressions Expressions, Expression Expression) __ = default
        ) : Node
        ;
    public partial record ReturnStatement(
        [Pattern("return","","",";")]
        (string RETURN,_ Spaces, Expressions Expressions, string SEMICOLON) _ = default
        ) : Statement
        ;
    public partial record Statements(
        [Pattern]Statement Statement = default,
        [Pattern](Statements Statements, Statement Statement) __ = default
        ) : Node
        ;
    public partial record Block(
        [Pattern(null,null)](Statements Statements, ReturnStatement Return) _ = default
        ) : Node
        ;
    public partial record Chunk(
        [Pattern(null,null)](Block Block, _ Spaces) _ = default
        ) : Node
        ;
}
