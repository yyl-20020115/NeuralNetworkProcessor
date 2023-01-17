/**
  * Copyright (c) 2013 Stefan Marr,   stefan.marr@vub.ac.be
 * Copyright (c) 2009 Michael Haupt, michael.haupt@hpi.uni-potsdam.de
 * Software Architecture Group, Hasso Plattner Institute, Potsdam, Germany
 * http://www.hpi.uni-potsdam.de/swa/
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;
using System.Numerics;
using System.Text;
using static Som.Compiler.Symbol;

public class Parser
{
    protected string filename;
    protected Universe universe;
    protected ClassGenerationContext cgenc;
    protected Lexer lexer;


    protected Symbol sym;
    protected string text;
    protected Symbol nextSym;

    protected static BytecodeGenerator bcGen = new();
    protected static List<Symbol> singleOpSyms = new ();
    protected static List<Symbol> binaryOpSyms = new ();
    protected static List<Symbol> keywordSelectorSyms = new ();

    static Parser()
    {
        foreach (Symbol s in new Symbol[] {
            Not, And, Or, Star, Div, Mod, Plus, Equal, More, Less, Comma, At, Per, NONE})
        {
            singleOpSyms.Add(s);
        }
        foreach (Symbol s in new Symbol[] {Or, Comma, Minus, Equal, Not, And, Or, Star,
            Div, Mod, Plus, Equal, More, Less, Comma, At, Per, NONE})
        {
            binaryOpSyms.Add(s);
        }
        foreach (Symbol s in new Symbol[] { Symbol.Keyword, KeywordSequence })
        {
            keywordSelectorSyms.Add(s);
        }
    }

    public class ParseError : ProgramDefinitionError
    {
        protected int line;
        protected int column;

        protected string text;
        protected string rawBuffer;
        protected string fileName;
        protected Symbol expected;
        protected Symbol found;

        public ParseError(string message, Symbol expected, Parser parser) : base(message)
        {
            if (parser.lexer == null)
            {
                this.line = 0;
                this.column = 0;
                this.rawBuffer = "";
            }
            else
            {
                this.line = parser.lexer.CurrentLineNumber;
                this.column = parser.lexer.CurrentColumn;
                this.rawBuffer = parser.lexer.RawBuffer;
            }
            this.text = parser.text;
            this.fileName = parser.filename;
            this.expected = expected;
            this.found = parser.sym;
        }

        protected string ExpectedSymbolAsString => expected.ToString();

        public override string Message
        {
            get
            {
                var msg = base.Message;

                var foundStr = Parser.PrintableSymbol(found) ? found + " (" + text + ")" : found.ToString();
                var expectedStr = ExpectedSymbolAsString;

                msg = msg.Replace("%(expected)s", expectedStr);
                msg = msg.Replace("%(found)s", foundStr);

                return msg;
            }
        }

        public override string ToString()
        {
            var msg = "%(file)s:%(line)d:%(column)d: error: " + base.Message;
            var foundStr = Parser.PrintableSymbol(found) ? found + " (" + text + ")" : found.ToString();
            msg += ": " + rawBuffer;
            var expectedStr = ExpectedSymbolAsString;

            msg = msg.Replace("%(file)s", fileName);
            msg = msg.Replace("%(line)d", "" + line);
            msg = msg.Replace("%(column)d", "" + column);
            msg = msg.Replace("%(expected)s", expectedStr);
            msg = msg.Replace("%(found)s", foundStr);
            return msg;
        }
    }

    public Parser(TextReader reader, Universe universe, string filename)
    {
        this.universe = universe;
        this.filename = filename;
        this.cgenc = new (universe);

        sym = NONE;
        lexer = new (reader);
        nextSym = NONE;
        GetSymbolFromLexer();
    }

    public override string ToString() => filename + ":" + lexer.CurrentLineNumber + ":" + lexer.CurrentColumn;

    public ClassGenerationContext Classdef()
    {
        cgenc.Name = universe.SymbolFor(text);
        this.Expect(Symbol.Identifier);
        this.Expect(Equal);

        this.SuperClass();

        this.Expect(NewTerm);
        this.ClassBody();

        if (this.Accept(Separator))
        {
            this.cgenc.StartClassSide();
            this.ClassBody();
        }
        this.Expect(EndTerm);

        return this.cgenc;
    }

    private void ClassBody()
    {
        this.Fields();
        while (this.SymIsMethod())
        {
            var mgenc = new MethodGenerationContext(cgenc);
            mgenc.AddArgument("self");

            this.Method(mgenc);
            this.cgenc.AddMethod(mgenc.Assemble(universe));
        }
    }

    private bool SymIsMethod() 
        => sym == Symbol.Identifier 
        || sym == Symbol.Keyword
        || sym == OperatorSequence
        || SymIn(binaryOpSyms)
        ;

    private void SuperClass()
    {
        SSymbol superName;
        if (sym == Symbol.Identifier)
        {
            superName = universe.SymbolFor(text);
            Accept(Symbol.Identifier);
        }
        else
        {
            superName = universe.SymbolFor("Object");
        }
        cgenc.SetSuperName(superName);

        // Load the super class, if it is not nil (break the dependency cycle)
        if (superName.EmbeddedString!=("nil"))
            InitalizeFromSuperClass(superName);
    }

    private void InitalizeFromSuperClass(SSymbol superName)
    {
        var superClass = universe.LoadClass(superName);
        if (superClass == null)
            throw new ParseError(
                "Was not able to load super class: " + superName.EmbeddedString,
                Symbol.NONE, this);
        cgenc.SetInstanceFieldsOfSuper(superClass.InstanceFields);
        cgenc.SetClassFieldsOfSuper(superClass.SOMClass.InstanceFields);
    }

    protected bool SymIn(List<Symbol> ss) => ss.Contains(sym);

    protected bool Accept(Symbol s)
    {
        if (sym == s)
        {
            GetSymbolFromLexer();
            return true;
        }
        return false;
    }

    protected bool AcceptOneOf(List<Symbol> ss)
    {
        if (SymIn(ss))
        {
            GetSymbolFromLexer();
            return true;
        }
        return false;
    }

    protected bool Expect(Symbol s)
    {
        if (Accept(s)) return true;
        var err = new StringBuilder("Error: " + filename + ":" +
            lexer.CurrentLineNumber +
            ": unexpected symbol, expected: " + s.ToString()
            + ", but found: " + sym.ToString());
        if (PrintableSymbol(sym))
        {
            err.Append(" (" + text + ")");
        }
        err.Append(": " + lexer.RawBuffer);
        throw new IllegalStateException(err.ToString());
    }

    protected bool ExpectOneOf(List<Symbol> ss)
    {
        if (AcceptOneOf(ss)) return true;
        var err = new StringBuilder("Error: " + filename + ":" +
            lexer.CurrentLineNumber + ": unexpected symbol, expected one of: ");
        foreach (Symbol s in ss)
        {
            err.Append(s.ToString() + ", ");
        }
        err.Append("but found: " + sym.ToString());
        if (PrintableSymbol(sym))
        {
            err.Append(" (" + text + ")");
        }
        err.Append(": " + lexer.RawBuffer);
        throw new IllegalStateException(err.ToString());
    }

    protected void Fields()
    {
        if (Accept(Or))
        {
            while (sym == Symbol.Identifier)
            {
                var var = Variable;
                cgenc.AddField(universe.SymbolFor(var));
            }
            Expect(Or);
        }
    }

    protected void Method(MethodGenerationContext mgenc)
    {
        Pattern(mgenc);
        Expect(Equal);
        if (sym == Primitive)
        {
            mgenc.MarkAsPrimitive();
            PrimitiveBlock();
        }
        else
        {
            MethodBlock(mgenc);
        }
    }

    protected void PrimitiveBlock() => Expect(Primitive);

    private void Pattern(MethodGenerationContext mgenc)
    {
        switch (sym)
        {
            case Symbol.Identifier:
                UnaryPattern(mgenc);
                break;
            case Symbol.Keyword:
                KeywordPattern(mgenc);
                break;
            default:
                BinaryPattern(mgenc);
                break;
        }
    }

    protected void UnaryPattern(MethodGenerationContext mgenc) => mgenc.SetSignature(UnarySelector());

    protected void BinaryPattern(MethodGenerationContext mgenc)
    {
        mgenc.SetSignature(BinarySelector());
        mgenc.AddArgumentIfAbsent(Argument);
    }

    protected void KeywordPattern(MethodGenerationContext mgenc)
    {
        var kw = new StringBuilder();
        do
        {
            kw.Append(Keyword());
            mgenc.AddArgumentIfAbsent(Argument);
        } while (sym == Symbol.Keyword);

        mgenc.SetSignature(universe.SymbolFor(kw.ToString()));
    }

    protected void MethodBlock(MethodGenerationContext mgenc)
    {
        Expect(NewTerm);
        BlockContents(mgenc);
        // if no return has been generated so far, we can be sure there was no .
        // terminating the last expression, so the last expression's value must
        // be popped off the stack and a ^self be generated
        if (!mgenc.IsFinished)
        {
            bcGen.EmitPOP(mgenc);
            bcGen.EmitPUSHARGUMENT(mgenc, (byte)0, (byte)0);
            bcGen.EmitRETURNLOCAL(mgenc);
            mgenc.SetFinished();
        }

        Expect(EndTerm);
    }

    protected SSymbol UnarySelector() => universe.SymbolFor(Identifier());

    protected SSymbol BinarySelector()
    {
        var s = text;

        // Checkstyle: stop @formatter:off
        if (Accept(Or))
        {
        }
        else if (Accept(Comma))
        {
        }
        else if (Accept(Minus))
        {
        }
        else if (Accept(Equal))
        {
        }
        else if (AcceptOneOf(singleOpSyms))
        {
        }
        else if (Accept(OperatorSequence))
        {
        }
        else { Expect(NONE); }
        // Checkstyle: resume @formatter:on

        return universe.SymbolFor(s);
    }

    protected string Identifier()
    {
        var s = text;
        var isPrimitive = Accept(Primitive);
        if (!isPrimitive) Expect(Symbol.Identifier);
        return s;
    }

    protected string Keyword()
    {
        var s = text;
        Expect(Symbol.Keyword);
        return s;
    }

    protected string Argument => Variable;

    protected void BlockContents(MethodGenerationContext mgenc)
    {
        if (Accept(Or))
        {
            Locals(mgenc);
            Expect(Or);
        }
        BlockBody(mgenc, false);
    }

    protected void Locals(MethodGenerationContext mgenc)
    {
        while (sym == Symbol.Identifier)
        {
            mgenc.AddLocalIfAbsent(Variable);
        }
    }

    protected void BlockBody(MethodGenerationContext mgenc, bool seenPeriod)
    {
        if (Accept(Exit))
        {
            Result(mgenc);
        }
        else if (sym == EndBlock)
        {
            if (seenPeriod)
            {
                // a POP has been generated which must be elided (blocks always
                // return the value of the last expression, regardless of
                // whether it
                // was terminated with a . or not)
                mgenc.RemoveLastBytecode();
            }
            if (mgenc.IsBlockMethod&& !mgenc.HasBytecodes)
            {
                // if the block is empty, we need to return nil
                var nilSym = universe.SymbolFor("nil");
                mgenc.AddLiteralIfAbsent(nilSym, this);
                bcGen.EmitPUSHGLOBAL(mgenc, nilSym);
            }
            bcGen.EmitRETURNLOCAL(mgenc);
            mgenc.SetFinished();
        }
        else if (sym == EndTerm)
        {
            // it does not matter whether a period has been seen, as the end of
            // the method has been found (EndTerm) - so it is safe to emit a "return
            // self"
            bcGen.EmitPUSHARGUMENT(mgenc, (byte)0, (byte)0);
            bcGen.EmitRETURNLOCAL(mgenc);
            mgenc.SetFinished();
        }
        else
        {
            Expression(mgenc);
            if (Accept(Period))
            {
                bcGen.EmitPOP(mgenc);
                BlockBody(mgenc, true);
            }
        }
    }

    protected void Result(MethodGenerationContext mgenc)
    {
        Expression(mgenc);

        if (mgenc.IsBlockMethod)
        {
            bcGen.EmitRETURNNONLOCAL(mgenc);
        }
        else
        {
            bcGen.EmitRETURNLOCAL(mgenc);
        }

        mgenc.MarkAsFinished();
        Accept(Period);
    }

    protected void Expression(MethodGenerationContext mgenc)
    {
        PeekForNextSymbolFromLexer();

        if (nextSym == Assign)
        {
            Assignation(mgenc);
        }
        else
        {
            Evaluation(mgenc);
        }
    }

    protected void Assignation(MethodGenerationContext mgenc)
    {
        List<string> list = new();

        Assignments(mgenc, list);
        Evaluation(mgenc);

        for (int i = 1; i <= list.Count; i++)
        {
            bcGen.EmitDUP(mgenc);
        }
        foreach (string s in list)
        {
            GeneratePopVariable(mgenc, s);
        }
    }

    protected void Assignments(MethodGenerationContext mgenc, List<string> l)
    {
        if (sym == Symbol.Identifier)
        {
            l.Add(Assignment(mgenc));
            PeekForNextSymbolFromLexer();
            if (nextSym == Assign)
            {
                Assignments(mgenc, l);
            }
        }
    }

    protected string Assignment(MethodGenerationContext mgenc)
    {
        var v = Variable;
        Expect(Assign);
        return v;
    }

    protected void Evaluation(MethodGenerationContext mgenc)
    {
        var superSend = Primary(mgenc);
        if (SymIsMethod()) Messages(mgenc, superSend);
    }

    protected bool Primary(MethodGenerationContext mgenc)
    {
        bool superSend = false;
        switch (sym)
        {
            case Symbol.Identifier:
                {
                    var v = Variable;
                    if (v == ("super"))
                    {
                        superSend = true;
                        // sends to super push self as the receiver
                        v = "self";
                    }

                    GeneratePushVariable(mgenc, v);
                    break;
                }
            case NewTerm:
                NestedTerm(mgenc);
                break;
            case NewBlock:
                {
                    var bgenc = new MethodGenerationContext(mgenc.Holder, mgenc);
                    NestedBlock(bgenc);

                    var blockMethod = bgenc.AssembleMethod(universe);
                    mgenc.AddLiteral(blockMethod, this);
                    bcGen.EmitPUSHBLOCK(mgenc, blockMethod);
                    break;
                }
            default:
                Literal(mgenc);
                break;
        }
        return superSend;
    }

    protected string Variable => Identifier();

    protected void Messages(MethodGenerationContext mgenc, bool superSend)
    {
        if (sym == Symbol.Identifier)
        {
            do
            {
                // only the first message in a sequence can be a super send
                UnaryMessage(mgenc, superSend);
                superSend = false;
            } while (sym == Symbol.Identifier);

            while (sym == OperatorSequence || SymIn(binaryOpSyms))
            {
                BinaryMessage(mgenc, false);
            }

            if (sym == Symbol.Keyword)
            {
                KeywordMessage(mgenc, false);
            }
        }
        else if (sym == OperatorSequence || SymIn(binaryOpSyms))
        {
            do
            {
                // only the first message in a sequence can be a super send
                BinaryMessage(mgenc, superSend);
                superSend = false;
            } while (sym == OperatorSequence || SymIn(binaryOpSyms));

            if (sym == Symbol.Keyword)
            {
                KeywordMessage(mgenc, false);
            }
        }
        else
        {
            KeywordMessage(mgenc, superSend);
        }
    }

    protected void UnaryMessage(MethodGenerationContext mgenc,bool superSend)
    {
        var msg = UnarySelector();
        mgenc.AddLiteralIfAbsent(msg, this);

        if (superSend)
        {
            bcGen.EmitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.EmitSEND(mgenc, msg);
        }
    }

    protected void BinaryMessage(MethodGenerationContext mgenc, bool superSend)
    {
        var msg = BinarySelector();
        mgenc.AddLiteralIfAbsent(msg, this);

        BinaryOperand(mgenc);

        if (superSend)
        {
            bcGen.EmitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.EmitSEND(mgenc, msg);
        }
    }

    protected bool BinaryOperand(MethodGenerationContext mgenc)
    {
        bool superSend = this.Primary(mgenc);

        while (sym == Symbol.Identifier)
        {
            this.UnaryMessage(mgenc, superSend);
            superSend = false;
        }

        return superSend;
    }

    protected void KeywordMessage(MethodGenerationContext mgenc, bool superSend)
    {
        var kw = new StringBuilder();
        do
        {
            kw.Append(Keyword());
            Formula(mgenc);
        } while (sym == Symbol.Keyword);

        var msg = universe.SymbolFor(kw.ToString());

        mgenc.AddLiteralIfAbsent(msg, this);

        if (superSend)
        {
            bcGen.EmitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.EmitSEND(mgenc, msg);
        }
    }

    protected void Formula(MethodGenerationContext mgenc)
    {
        var superSend = BinaryOperand(mgenc);

        // only the first message in a sequence can be a super send
        if (sym == OperatorSequence || SymIn(binaryOpSyms))
        {
            BinaryMessage(mgenc, superSend);
        }

        while (sym == OperatorSequence || SymIn(binaryOpSyms))
        {
            BinaryMessage(mgenc, false);
        }
    }

    protected void NestedTerm(MethodGenerationContext mgenc)
    {
        this.Expect(NewTerm);
        this.Expression(mgenc);
        this.Expect(EndTerm);
    }

    protected void Literal(MethodGenerationContext mgenc)
    {
        switch (sym)
        {
            case Pound:
                {
                    this.PeekForNextSymbolFromLexerIfNecessary();
                    if (nextSym == NewTerm)
                    {
                        this.LiteralArray(mgenc);
                    }
                    else
                    {
                        this.LiteralSymbol(mgenc);
                    }
                    break;
                }
            case STString:
                {
                    LiteralString(mgenc);
                    break;
                }
            default:
                {
                    LiteralNumber(mgenc);
                    break;
                }
        }
    }

    protected void LiteralNumber(MethodGenerationContext mgenc)
    {
        var lit = sym == Minus ? NegativeDecimal() : LiteralDecimal(false);
        mgenc.AddLiteralIfAbsent(lit, this);
        bcGen.EmitPUSHCONSTANT(mgenc, lit);
    }

    protected SAbstractObject LiteralDecimal(bool isNegative) => sym == Integer ? LiteralInteger(isNegative) : LiteralDouble(isNegative);

    protected SAbstractObject NegativeDecimal()
    {
        this.Expect(Minus);
        return this.LiteralDecimal(true);
    }

    protected SAbstractObject LiteralInteger(bool isNegative)
    {
        if (long.TryParse(text, out var i))
        {
            if (isNegative) i = -i;
            this.Expect(Integer);
            return this.universe.NewInteger(i);
        }
        else
        {
            if (BigInteger.TryParse(text, out var big))
            {
                if (isNegative) big = -big;
                this.Expect(Integer);
                return this.universe.NewBigInteger(big);
            }
            else
            {
                var err = "Error: " + filename + ":" +
                    lexer.CurrentLineNumber +
                    ": parsing number literal failed: '" + text.ToString()
                    + "'";
                throw new IllegalStateException(err);
            }
        }
    }

    protected SAbstractObject LiteralDouble(bool isNegative)
    {
        if (double.TryParse(text, out var d))
        {
            if (isNegative) d = -d;
            this.Expect(Double);
            return this.universe.NewDouble(d);
        }
        else
        {
            throw new ParseError("Could not parse double. Expected a number but " +
                "got '" + text + "'", NONE, this);
        }
    }

    protected void LiteralSymbol(MethodGenerationContext mgenc)
    {
        Expect(Pound);
        var symb = sym == STString ? universe.SymbolFor(GetString()) : Selector;
        mgenc.AddLiteralIfAbsent(symb, this);
        bcGen.EmitPUSHCONSTANT(mgenc, symb);
    }

    protected void LiteralString(MethodGenerationContext mgenc)
    {
        var s = this.GetString();
        var str = this.universe.NewString(s);
        mgenc.AddLiteralIfAbsent(str, this);
        bcGen.EmitPUSHCONSTANT(mgenc, str);
    }

    protected void LiteralArray(MethodGenerationContext mgenc)
    {
        this.Expect(Pound);
        this.Expect(NewTerm);
        var arrayClassName = universe.SymbolFor("Array");
        var arraySizePlaceholder = universe.SymbolFor("ArraySizeLiteralPlaceholder");
        var newMessage = universe.SymbolFor("new:");
        var atPutMessage = universe.SymbolFor("at:put:");

        mgenc.AddLiteralIfAbsent(arrayClassName, this);
        mgenc.AddLiteralIfAbsent(newMessage, this);
        mgenc.AddLiteralIfAbsent(atPutMessage, this);
        byte arraySizeLiteralIndex = mgenc.AddLiteral(arraySizePlaceholder, this);

        // create empty array
        bcGen.EmitPUSHGLOBAL(mgenc, arrayClassName);
        bcGen.EmitPUSHCONSTANT(mgenc, arraySizeLiteralIndex);
        bcGen.EmitSEND(mgenc, newMessage);

        int i = 1;

        while (sym != EndTerm)
        {
            var pushIndex = universe.NewInteger(i);
            mgenc.AddLiteralIfAbsent(pushIndex, this);
            bcGen.EmitPUSHCONSTANT(mgenc, pushIndex);
            Literal(mgenc);
            bcGen.EmitSEND(mgenc, atPutMessage);
            i += 1;
        }

        // replace the placeholder with the actual array size
        mgenc.UpdateLiteral(
            arraySizePlaceholder, arraySizeLiteralIndex, universe.NewInteger(i - 1));
        Expect(EndTerm);
    }

    protected SSymbol Selector =>
            sym == OperatorSequence || SymIn(singleOpSyms)
            ? BinarySelector()
            : sym == Symbol.Keyword
            || sym == KeywordSequence
            ? KeywordSelector()
            : UnarySelector()
        ;

    protected SSymbol KeywordSelector()
    {
        var s = new string(text);
        ExpectOneOf(keywordSelectorSyms);
        return universe.SymbolFor(s);
    }

    protected string GetString() 
    {
        var s = new string(this.text);
        Expect(STString);
        return s;
    }

    protected void NestedBlock(MethodGenerationContext mgenc)
    {
        mgenc.AddArgumentIfAbsent("$block self");

        Expect(NewBlock);
        if (sym == Colon) this.BlockPattern(mgenc);

        // generate Block signature
        var blockSig = "$block method";
        int argSize = mgenc.NumberOfArguments;
        for (int i = 1; i < argSize; i++) blockSig += ":";

        mgenc.SetSignature(universe.SymbolFor(blockSig));

        this.BlockContents(mgenc);

        // if no return has been generated, we can be sure that the last
        // expression
        // in the block was not terminated by ., and can generate a return
        if (!mgenc.IsFinished)
        {
            if (!mgenc.HasBytecodes)
            {
                // if the block is empty, we need to return nil
                var nilSym = universe.SymbolFor("nil");
                mgenc.AddLiteralIfAbsent(nilSym, this);
                bcGen.EmitPUSHGLOBAL(mgenc, nilSym);
            }
            bcGen.EmitRETURNLOCAL(mgenc);
            mgenc.MarkAsFinished();
        }

        this.Expect(EndBlock);
    }

    protected void BlockPattern(MethodGenerationContext mgenc)
    {
        this.BlockArguments(mgenc);
        this.Expect(Or);
    }

    protected void BlockArguments(MethodGenerationContext mgenc)
    {
        do
        {
            Expect(Colon);
            mgenc.AddArgumentIfAbsent(Argument);
        } while (sym == Colon);
    }

    protected void GeneratePushVariable(MethodGenerationContext mgenc, string var)
    {
        // The purpose of this function is to find out whether the variable to be
        // pushed on the stack is a local variable, argument, or object field.
        // This is done by examining all available lexical contexts, starting with
        // the innermost (i.e., the one represented by mgenc).

        // triplet: index, context, isArgument
        Triplet<byte, byte, bool> tri = new (0, 0, false);

        if (mgenc.FindVariable(var, tri))
        {
            if (tri.Z)
            {
                bcGen.EmitPUSHARGUMENT(mgenc, tri.X, tri.Y);
            }
            else
            {
                bcGen.EmitPUSHLOCAL(mgenc, tri.X, tri.Y);
            }
        }
        else
        {
            var identifier = universe.SymbolFor(var);
            if (mgenc.HasField(identifier))
            {
                var fieldName = identifier;
                mgenc.AddLiteralIfAbsent(fieldName, this);
                bcGen.EmitPUSHFIELD(mgenc, fieldName);
            }
            else
            {
                var global = identifier;
                mgenc.AddLiteralIfAbsent(global, this);
                bcGen.EmitPUSHGLOBAL(mgenc, global);
            }
        }
    }

    protected void GeneratePopVariable(MethodGenerationContext mgenc, string var)
    {
        // The purpose of this function is to find out whether the variable to be
        // popped off the stack is a local variable, argument, or object field.
        // This is done by examining all available lexical contexts, starting with
        // the innermost (i.e., the one represented by mgenc).

        // triplet: index, context, isArgument
        Triplet<byte, byte, bool> tri = new (0, 0, false);

        if (mgenc.FindVariable(var, tri))
        {
            if (tri.Z)
            {
                bcGen.EmitPOPARGUMENT(mgenc, tri.X, tri.Y);
            }
            else
            {
                bcGen.EmitPOPLOCAL(mgenc, tri.X, tri.Y);
            }
        }
        else
        {
            var varName = universe.SymbolFor(var);
            if (!mgenc.HasField(varName))
            {
                throw new ParseError("Trying to write to field with the name '" + var
                    + "', but field does not seem exist in class.", Symbol.NONE, this);
            }
            bcGen.EmitPOPFIELD(mgenc, varName);
        }
    }

    protected void GetSymbolFromLexer()
    {
        sym = lexer.GetSymbol();
        text = lexer.Text;
    }

    protected void PeekForNextSymbolFromLexerIfNecessary()
    {
        if (!lexer.PeekDone) PeekForNextSymbolFromLexer();
    }

    protected void PeekForNextSymbolFromLexer() => nextSym = lexer.Peek();

    protected static bool PrintableSymbol(Symbol symbol) 
        => symbol == Integer || symbol == Double || symbol.CompareTo(STString) >= 0;
}
