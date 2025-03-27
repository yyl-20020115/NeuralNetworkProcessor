namespace NeuralNetworkProcessorSample.Samples.Calculator;

public static class ExpressionGenerator
{
    public abstract class Expression(bool useParentheses = false)
    {
        public bool UseParentheses = useParentheses;

        public abstract double Calculate();
    }
    public abstract class UnaryExpression(string @operator, ExpressionGenerator.Expression operand, bool useParentheses = false) : Expression(useParentheses)
    {
        public string Operator = @operator;
        public Expression Operand = operand;

        public override string ToString() =>
            UseParentheses
                ? ($"({this.Operator} {this.Operand})")
                : ($"{this.Operator} {this.Operand}");
    }
    public class ValueExpression(double value, bool useParentheses = false) : Expression(useParentheses)
    {
        public double Value = value;

        public override string ToString() =>
            UseParentheses
                ? $"({this.Value})"
                : $"{this.Value}";
        public override double Calculate() => this.Value;
    }
    public class PlusExpression(Expression operand, bool useParentheses = false) : UnaryExpression("+",operand,useParentheses)
    {
        public override double Calculate() => this.Operand.Calculate();
        public override string ToString() =>
            UseParentheses
                ? $"({this.Operand})"
                : $"{this.Operand}";
    }
    public class MinusExpression(Expression operand, bool useParentheses = false) : UnaryExpression("-", operand, useParentheses)
    {
        public override double Calculate() => -this.Operand.Calculate();
        public override string ToString() =>
            UseParentheses
                ? $"(-{this.Operand})"
                : $"-{this.Operand}";
    }

    public abstract class BinaryExpression(string @operator, ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : Expression(useParentheses)
    {
        public string Operator = @operator;
        public Expression LeftOperand = leftOperand;
        public Expression RightOperand = rightOperand;

        public override string ToString()
            =>
            UseParentheses
            ? $"({LeftOperand} {this.Operator} {this.RightOperand})"
            : $"{LeftOperand} {this.Operator} {this.RightOperand}"
            ;
    }

    public class AddExpression(ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : BinaryExpression("+",leftOperand,rightOperand,useParentheses)
    {
        public override double Calculate()
            => this.LeftOperand.Calculate() + this.RightOperand.Calculate();
    }
    public class SubExpression(ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : BinaryExpression("-", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate()
            => this.LeftOperand.Calculate() - this.RightOperand.Calculate();
    }
    public class MulExpression(ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : BinaryExpression("*", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate()
            => this.LeftOperand.Calculate() * this.RightOperand.Calculate();
    }
    public class DivExpression(ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : BinaryExpression("/", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate()
            => this.LeftOperand.Calculate() / this.RightOperand.Calculate();
    }
    public class PowerExpression(ExpressionGenerator.Expression leftOperand, ExpressionGenerator.Expression rightOperand, bool useParentheses = false) : BinaryExpression("^", leftOperand, rightOperand, useParentheses)
    {
        public override double Calculate()
            => Math.Pow(this.LeftOperand.Calculate(), this.RightOperand.Calculate());
    }

    public static readonly Random Random = Random.Shared;

    public static Expression CreateValueExpression(double value, bool useParentheses = false)
        => new ValueExpression(value, useParentheses);
    public static Expression CreateUnaryExpression(int choice, Expression operand, bool useParentheses = false) => choice switch
    {
        0 => new PlusExpression(operand, useParentheses),
        _ => new MinusExpression(operand, useParentheses)
    };
    public static Expression CreateBinaryExpression(int choice, Expression leftOperand, Expression rightOperand, bool useParentheses = false) => choice switch
    {
        0 => new AddExpression(leftOperand, rightOperand, useParentheses),
        1 => new SubExpression(leftOperand, rightOperand, useParentheses),
        2 => new MulExpression(leftOperand, rightOperand, useParentheses),
        3 => new DivExpression(leftOperand, rightOperand, useParentheses),
        4 => new PowerExpression(leftOperand, rightOperand, useParentheses),
    };
    public static Expression CreateRandomExpression(int depth)
        => CreateRandomExpression(depth, Random.Next(2) == 0);

    public static Expression CreateRandomExpression(int depth,bool useParentheses = true) =>
        depth <=0 ? CreateValueExpression(Random.NextDouble(), useParentheses)
        : Random.Next(3) switch {
            0 => CreateValueExpression(Random.NextDouble(), useParentheses),
            1 => CreateUnaryExpression(Random.Next(2), CreateRandomExpression(depth - 1), useParentheses),
            2 => CreateBinaryExpression(Random.Next(5), CreateRandomExpression(depth - 1), CreateRandomExpression(depth - 1), useParentheses),
            _ => CreateValueExpression(Random.NextDouble(), useParentheses)
        };
}
