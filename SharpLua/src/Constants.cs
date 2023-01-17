namespace SharpLua
{
    public enum LuaType : int
    {
        Nil = 0,
        Bool = 1,
        Number = 3,
        String = 4,
    }

    public abstract class Constant
    {
        public LuaType Type
        {
            get;
            protected set;
        }

        public abstract override string ToString();
    }

    public class Constant<T> : Constant
    {
        public T Value
        {
            get;
            private set;
        }

        protected Constant(LuaType type, T value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }

    public class NilConstant : Constant<object>
    {
        public NilConstant() : base(LuaType.Nil, null) { }

        public override string ToString() => "nil";
    }

    public class BoolConstant : Constant<bool>
    {
        public BoolConstant(bool value) : base(LuaType.Bool, value) { }

        public override string ToString() => Value ? "true" : "false";
    }

    public class NumberConstant : Constant<double>
    {
        public NumberConstant(double value) : base(LuaType.Number, value) { }
    }

    public class StringConstant : Constant<string>
    {
        public StringConstant(string value) : base(LuaType.String, value) { }

        public override string ToString() => '\"' + (Value ?? "") + '\"';
    }
}
