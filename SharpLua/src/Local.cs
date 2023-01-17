// Representation of a Lua local variable

namespace SharpLua
{
    public class Local
    {
        public string Name
        {
            get;
            private set;
        }

        public int ScopeStart
        {
            get;
            private set;
        }

        public int ScopeEnd
        {
            get;
            private set;
        }

        public Local(string name, int scopeStart, int scopeEnd)
        {
            Name = name;
            ScopeStart = scopeStart;
            ScopeEnd = scopeEnd;
        }
    }
}
