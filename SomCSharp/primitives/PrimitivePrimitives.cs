namespace Som.Primitives;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;


public class PrimitivePrimitives : Primitives
{
    public PrimitivePrimitives(Universe universe) : base(universe) { }
    public class HolderPrimitive : SPrimitive
    {
        public HolderPrimitive(Universe universe)
            : base("holder", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SPrimitive)frame.Pop();
            frame.Push(self.Holder);
        }
    }
    public class SignaturePrimitive : SPrimitive
    {
        public SignaturePrimitive(Universe universe)
            : base("signature", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SPrimitive)frame.Pop();
            frame.Push(self.Signature);
        }
    }

    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new HolderPrimitive(universe));
        this.InstallInstancePrimitive(new SignaturePrimitive(universe));
    }
}
