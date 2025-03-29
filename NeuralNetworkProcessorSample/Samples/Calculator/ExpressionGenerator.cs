namespace NeuralNetworkProcessorSample.Samples.Calculator;

public static class ExpressionGenerator
{
    public abstract class Expression(bool useParentheses = false)
    {
        public static string TrimDoubleParentheses(string text)
            => text.StartsWith("((") && text.EndsWith("))")
            ? TrimDoubleParentheses(text[1..^1])
            : text
            ;

        public bool UseParentheses = useParentheses;
        public Expression? Parent;

        public abstract double Calculate(Dictionary<string, double> dict);
    }

    public abstract class UnaryExpression
        : Expression
    {
        public string Operator;
        public Expression Operand;
        protected UnaryExpression(string @operator, Expression operand, bool useParentheses = false)
            : base(useParentheses)
        {
            this.Operator = @operator;
            this.Operand = operand;
            this.Operand.Parent = this; 
        }
        public override string ToString() =>
            UseParentheses && this.Parent != null
                ? $"({this.Operator} {this.Operand})"
                : ($"{this.Operator} {this.Operand}")
            ;
    }

    public class DoubleExpression(double value)
        : Expression(false)
    {
        public double Value = value;

        public override string ToString()
            => $"{this.Value}"
            ;

        public override double Calculate(Dictionary<string, double> dict)
            => this.Value
            ;
    }

    public class IntExpression(int value)
        : Expression(false)
    {
        public int Value = value;

        public override string ToString()
            => $"{this.Value}"
            ;

        public override double Calculate(Dictionary<string, double> dict)
            => this.Value
            ;
    }
    public class VariableExpression(string name)
        : Expression(false)
    {
        public string Name = name;
        public override string ToString()
            => this.Name
            ;

        public override double Calculate(Dictionary<string, double> dict)
            => dict.TryGetValue(this.Name, out var value)
            ? value
            : 0
            ;
    }

    public class PlusExpression(Expression operand, bool useParentheses = false)
        : UnaryExpression("+", operand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
            => this.Operand.Calculate(dict)
            ;

        public override string ToString()
            => UseParentheses && this.Parent != null && this.Operand is not VariableExpression
                ? TrimDoubleParentheses($"({this.Operand})")
                :($"{this.Operand}")
            ;
    }
    public class MinusExpression(Expression operand, bool useParentheses = false)
        : UnaryExpression("-", operand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
            => -this.Operand.Calculate(dict)
            ;

        public override string ToString()
            => UseParentheses && this.Parent != null
                ? TrimDoubleParentheses($"(-{this.Operand})")
                : $"-{this.Operand}"
            ;
    }

    public abstract class BinaryExpression
        :Expression
    {
        public string Operator;
        public Expression LeftOperand;
        public Expression RightOperand;

        protected BinaryExpression(string @operator, Expression leftOperand, Expression rightOperand, bool useParentheses = false)
            :base(useParentheses)
        {
            this.Operator = @operator;
            this.LeftOperand = leftOperand;
            this.RightOperand = rightOperand;
            this.RightOperand.Parent = this;
            this.LeftOperand.Parent = this;
        }

        public override string ToString()
            => UseParentheses && this.Parent != null
            ? TrimDoubleParentheses($"({LeftOperand} {this.Operator} {this.RightOperand})")
            : $"{LeftOperand} {this.Operator} {this.RightOperand}"
            ;
    }

    public class AddExpression(Expression leftOperand, Expression rightOperand, bool useParentheses = false)
        : BinaryExpression("+", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
            => this.LeftOperand.Calculate(dict)
            + this.RightOperand.Calculate(dict)
            ;
    }
    public class SubExpression(Expression leftOperand, Expression rightOperand, bool useParentheses = false)
        : BinaryExpression("-", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
            => this.LeftOperand.Calculate(dict)
            - this.RightOperand.Calculate(dict)
            ;
    }
    public class MulExpression(Expression leftOperand, Expression rightOperand, bool useParentheses = false)
        : BinaryExpression("*", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
            => this.LeftOperand.Calculate(dict)
            * this.RightOperand.Calculate(dict)
            ;
    }
    public class DivExpression(Expression leftOperand, Expression rightOperand, bool useParentheses = false)
        : BinaryExpression("/", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate(Dictionary<string, double> dict)
        {
            var left = this.LeftOperand.Calculate(dict);
            var right = this.RightOperand.Calculate(dict);
            return right != 0 ? left / right : double.NaN;
        }
    }

    public static Expression GenerateDoubleExpression(double value)
        => new DoubleExpression(value);

    public static Expression GenerateIntExpression(int value)
        => new IntExpression(value);

    public static Expression GenerateVariableExpression(Dictionary<string, double> dict, string name, double value = 1.0)
    {
        dict[name] = value;
        return new VariableExpression(name);
    }
    public static Expression GenerateUnaryExpression(int choice, Expression operand, bool useParentheses = false) => choice switch
    {
        0 => new PlusExpression(operand, useParentheses),
        _ => new MinusExpression(operand, useParentheses)
    };

    public static Expression GenerateBinaryExpression(int choice, Expression leftOperand, Expression rightOperand, bool useParentheses = false) => choice switch
    {
        0 => new AddExpression(leftOperand, rightOperand, useParentheses),
        1 => new SubExpression(leftOperand, rightOperand, useParentheses),
        2 => new MulExpression(leftOperand, rightOperand, useParentheses),
        3 => new DivExpression(leftOperand, rightOperand, useParentheses),
        _ => throw new NotImplementedException(),
    };

    public static readonly Random Random = Random.Shared;

    public static Expression GenerateRandomDoubleExpressionTree(Dictionary<string, double> dict, int depth = 1, Random? _Random = null)
        => GenerateRandomDoubleExpressionTree(dict, depth, (_Random ?? Random).Next(2) == 0)
        ;

    public static Expression GenerateRandomDoubleExpressionTree(Dictionary<string, double> dict, int depth = 1, bool useParentheses = true, Random? _Random = null) =>
        depth <= 1 ?
        (_Random ?? Random).Next(2) switch
        {
            0 => GenerateDoubleExpression((_Random ?? Random).Next()),
            1 => GenerateVariableExpression(dict, ((char)('a' + (_Random ?? Random).Next(26))).ToString(), 1.0),
            _ => throw new NotImplementedException(),
        }
        : (_Random ?? Random).Next(2) switch
        {
            0 => GenerateUnaryExpression((_Random ?? Random).Next(2), GenerateRandomDoubleExpressionTree(dict, depth - 1, useParentheses, _Random), useParentheses),
            1 => GenerateBinaryExpression((_Random ?? Random).Next(4), GenerateRandomDoubleExpressionTree(dict, depth - 1, useParentheses, _Random), GenerateRandomDoubleExpressionTree(dict, depth - 1, useParentheses, _Random), useParentheses),
            _ => throw new NotImplementedException(),
        };

    public static Expression GenerateRandomIntExpressionTree(Dictionary<string, double> dict, int depth = 1, double defaultVariableValue = 1.0, Random? _Random = null)
        => GenerateRandomIntExpressionTree(dict, depth, defaultVariableValue, (_Random ?? Random).Next(2) == 0)
        ;

    public static Expression GenerateRandomIntExpressionTree(Dictionary<string, double> dict, int depth = 1, double defaultVariableValue = 1.0, bool useParentheses = true, Random? _Random = null) =>
        depth <= 1 ?
        (_Random ?? Random).Next(2) switch
        {
            0 => GenerateIntExpression((_Random ?? Random).Next()),
            1 => GenerateVariableExpression(dict, ('a' + (_Random ?? Random).Next(26)).ToString(), defaultVariableValue),
            _ => throw new NotImplementedException(),
        }
        : (_Random ?? Random).Next(2) switch
        {
            0 => GenerateUnaryExpression((_Random ?? Random).Next(2), GenerateRandomIntExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), useParentheses),
            1 => GenerateBinaryExpression((_Random ?? Random).Next(4), GenerateRandomIntExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), GenerateRandomIntExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), useParentheses),
            _ => throw new NotImplementedException(),
        };

    public static Expression GenerateRandomVariableExpressionTree(Dictionary<string, double> dict, int depth = 1, double defaultVariableValue = 1.0, bool useParentheses = true, Random? _Random = null) =>
        depth <= 1 ?
        GenerateVariableExpression(dict, ((char)('a' + (_Random ?? Random).Next(26))).ToString(), 1.0)
        : (_Random ?? Random).Next(2) switch
        {
            0 => GenerateUnaryExpression((_Random ?? Random).Next(2), GenerateRandomVariableExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), useParentheses),
            1 => GenerateBinaryExpression((_Random ?? Random).Next(4), GenerateRandomVariableExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), GenerateRandomVariableExpressionTree(dict, depth - 1, defaultVariableValue, useParentheses, _Random), useParentheses),
            _ => throw new NotImplementedException(),
        };
}
