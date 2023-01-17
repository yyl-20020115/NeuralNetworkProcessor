/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
 * Copyright (c) 2016 Michael Haupt, github@haupz.de
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

namespace Som.VM;
using Som.Compiler;
using Som.Interpreter;
using Som.VMObject;
using System.Numerics;
using static Som.Interpreter.Bytecodes;

public class Universe
{
    /// <summary>
    /// dotnet run -- -cp core-lib/Smalltalk core-lib/TestSuite/TestHarness.som
    /// *nix:
    /// dotnet run -- -cp core-lib/Smalltalk:core-lib/TestSuite:core-lib/SomSom/src/compiler:core-lib/SomSom/src/vm:core-lib/SomSom/src/vmobjects:core-lib/SomSom/src/interpreter:core-lib/SomSom/src/primitives core-lib/SomSom/tests/SomSomTests.som
    /// Windows:
    /// dotnet run -- -cp core-lib/Smalltalk:core-lib/TestSuite:core-lib/SomSom/src/compiler:core-lib/SomSom/src/vm:core-lib/SomSom/src/vmobjects:core-lib/SomSom/src/interpreter:core-lib/SomSom/src/primitives core-lib/SomSom/tests/SomSomTests.som
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public static int Main(params string[] arguments)
    {
        var u = new Universe();

        // Start interpretation
        try
        {
            u.Interpret(arguments);
        }
        catch (ProgramDefinitionError e)
        {
            u.ErrorExit(e.ToString());
            return u.Exit(-1);
        }

        // Exit with error code 0
        return u.Exit(0);
    }

    public SAbstractObject Interpret(string[] arguments)
    {
        // Check for command line switches
        arguments = HandleArguments(arguments);

        // Initialize the known universe
        return Initialize(arguments);
    }

    public Universe(bool avoidExit = false)
    {
        this.interpreter = new (this);
        this.symbolTable = new ();
        this.avoidExit = avoidExit;
        this.lastExitCode = 0;

        current = this;
    }

    public static Universe Current => current;

    public Interpreter Interpreter => interpreter;

    public int Exit(int errorCode)
    {
        // Exit from the Java system
        if (!avoidExit)
        {
            Environment.Exit(errorCode);
        }
        else
        {
            lastExitCode = errorCode;
        }
        return errorCode;
    }

    public int LastExitCode() => lastExitCode;

    public void ErrorExit(string message)
    {
        ErrorPrintln("Runtime Error: " + message);
        Exit(1);
    }

    private string[] HandleArguments(string[] arguments)
    {
        var gotClasspath = false;
        List<string> remainingArgs = new();

        // read dash arguments only while we haven't seen other kind of arguments
        var sawOthers = false;

        for (int i = 0; i < arguments.Length; i++)
        {
            if ((arguments[i] == ("-cp")||arguments[i] == ("--cp")) && !sawOthers)
            {
                if (i + 1 >= arguments.Length)
                {
                    PrintUsageAndExit();
                }
                
                SetupClassPath(arguments[i + 1]);
                // Checkstyle: stop
                ++i; // skip class path
                     // Checkstyle: resume
                gotClasspath = true;
            }
            else if (arguments[i] == ("-d") && !sawOthers)
            {
                dumpBytecodes = true;
            }
            else
            {
                sawOthers = true;
                remainingArgs.Add(arguments[i]);
            }
        }

        if (!gotClasspath)
        {
            // Get the default class path of the appropriate size
            classPaths = SetupDefaultClassPath(0);
        }

        // check first of remaining args for class paths, and strip file extension

        if (remainingArgs.Count > 0)
        {
            var split = GetPathClassExt(remainingArgs[0]);

            if (!string.IsNullOrEmpty(split[0]))
            { // there was a path
                classPaths.Insert(0, split[0]);
            }
            remainingArgs[0]=split[1];
            var cps = new HashSet<string>();    
            foreach(var cp in classPaths)
            {
                var tcp = cp;
                if (!cp.StartsWith(Environment.CurrentDirectory))
                {
                    tcp = Path.Combine(Environment.CurrentDirectory, cp);
                }
                var fcp = new FileInfo(tcp);
                cps.Add(fcp.FullName);
            }
            this.classPaths = cps.ToList();
        }

        return remainingArgs.ToArray();
    }

    // take argument of the form "../foo/Test.som" and return
    // "../foo", "Test", "som"
    private string[] GetPathClassExt(string arg)
    {
        string full = "", name = "", ext = "";
        int i = arg.LastIndexOf('.');
        if (i >= 0)
        {
            ext = arg[(i + 1)..];
            arg = arg[..i];
        }
        i = arg.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        if (i >= 0)
        {
            full = arg[..(i + 1)];
            name = arg[(i + 1)..];

        }
        return new string[] { full, name, ext };
    }

    public void SetupClassPath(string cp)
    {
        var paths = cp.Split(pathSeparator);

        var retpaths = this.SetupDefaultClassPath(paths.Length);
        retpaths.AddRange(paths);
        foreach(var path in retpaths)
        {
            var newpath = path.Replace('/', fileSeparator);
            this.classPaths.Add(newpath);
            //Console.WriteLine(newpath);
        }
    }

    private List<string> SetupDefaultClassPath(int directories)
    {
        // Get the default system class path
        var systemClassPath = Environment.CurrentDirectory;

        // Compute the number of defaults
        int defaults = (systemClassPath != null) ? 2 : 1;

        // Allocate an array with room for the directories and the defaults
        var result = new List<string>();

        // Insert the system class path into the defaults section
        if (systemClassPath != null)
        {
            result.Add(Path.Combine(systemClassPath, "."));

            result.Add(Path.Combine(systemClassPath, "core-lib"+fileSeparator+"Smalltalk"));
        }

        result.Add(".");

        // Return the class path
        return result;
    }

    private void PrintUsageAndExit()
    {
        // Print the usage
        Println("Usage: som [-options] [args...]                          ");
        Println("                                                         ");
        Println("where options include:                                   ");
        Println("    -cp <directories separated by " + pathSeparator  + ">");
        Println("                  set search path for application classes");
        Println("    -d            enable disassembling");
        // Exit
        Environment.Exit(0);
    }

    /**
     * Start interpretation by sending the selector to the given class.
     * This is mostly meant for testing currently.
     *
     * @param className
     * @param selector
     * @return
     * @throws ProgramDefinitionError
     */
    public SAbstractObject Interpret(string className,string selector)
    {
        InitializeObjectSystem();

        var clazz = LoadClass(SymbolFor(className));

        // Lookup the initialize invokable on the system class
        var initialize =
            (SMethod)clazz.GetSOMClass(this).LookupInvokable(SymbolFor(selector));

        if (initialize == null) {
            throw new Exception("Lookup of " + className + ">>#" + selector + " failed");
        }

        return InterpretMethod(clazz, initialize, null);
    }

    private SAbstractObject Initialize(string[] arguments)
    {
        var systemObject = InitializeObjectSystem();
        // Start the shell if no filename is given
        if (arguments.Length == 0) {
            var shell = new Shell(this, interpreter);
            var bootstrapMethod = CreateBootstrapMethod();
            shell.SetBootstrapMethod(bootstrapMethod);
            return shell.Start();
        }

        // Lookup the initialize invokable on the system class
        var initialize = systemClass.LookupInvokable(SymbolFor("initialize:"));
        // Convert the arguments into an array
        var argumentsArray = NewArray(arguments);

        return InterpretMethod(systemObject, initialize,argumentsArray);
    }

    private SMethod CreateBootstrapMethod()
    {
        // Create a fake bootstrap method to simplify later frame traversal
        var bootstrapMethod = NewMethod(SymbolFor("bootstrap"), 1, 0, 2, null);
        bootstrapMethod.SetBytecode(0, HALT);
        bootstrapMethod.Holder = systemClass;
        return bootstrapMethod;
    }

    private SAbstractObject InterpretMethod(SAbstractObject receiver, ISInvokable invokable, SArray arguments)
    {
        var bootstrapMethod = CreateBootstrapMethod();
        // Create a fake bootstrap frame with the system object on the stack
        var bootstrapFrame = interpreter.PushNewFrame(bootstrapMethod);
        bootstrapFrame.Push(receiver);

        if (arguments != null) {
            bootstrapFrame.Push(arguments);
        }

        // Invoke the initialize invokable
        invokable.Invoke(bootstrapFrame, interpreter);
        // Start the interpreter
        return interpreter.Start();
    }

    private SAbstractObject InitializeObjectSystem()
    {
        // Allocate the nil object
        this.nilObject = new SObject(null);
        // Allocate the Metaclass classes
        this.metaclassClass = this.NewMetaclassClass();

        // Allocate the rest of the system classes
        this.objectClass = this.NewSystemClass();
        this.nilClass = this.NewSystemClass();
        this.classClass = this.NewSystemClass();
        this.arrayClass = this.NewSystemClass();
        this.symbolClass = this.NewSystemClass();
        this.methodClass = this.NewSystemClass();
        this.integerClass = this.NewSystemClass();
        this.primitiveClass = this.NewSystemClass();
        this.stringClass = this.NewSystemClass();
        this.doubleClass = this.NewSystemClass();

        // Setup the class reference for the nil object
        this.nilObject.SetClass(this.nilClass);

        // Initialize the system classes.
        this.InitializeSystemClass(this.objectClass, null, "Object");
        this.InitializeSystemClass(this.classClass, this.objectClass, "Class");
        this.InitializeSystemClass(this.metaclassClass, this.classClass, "Metaclass");
        this.InitializeSystemClass(this.nilClass, this.objectClass, "Nil");
        this.InitializeSystemClass(this.arrayClass, this.objectClass, "Array");
        this.InitializeSystemClass(this.methodClass, this.arrayClass, "Method");
        this.InitializeSystemClass(this.stringClass, this.objectClass, "String");
        this.InitializeSystemClass(this.symbolClass, this.stringClass, "Symbol");
        this.InitializeSystemClass(this.integerClass, this.objectClass, "Integer");
        this.InitializeSystemClass(this.primitiveClass, this.objectClass, "Primitive");
        this.InitializeSystemClass(this.doubleClass, this.objectClass, "Double");

        // Load methods and fields into the system classes
        this.LoadSystemClass(this.objectClass);
        this.LoadSystemClass(this.classClass);
        this.LoadSystemClass(this.metaclassClass);
        this.LoadSystemClass(this.nilClass);
        this.LoadSystemClass(this.arrayClass);
        this.LoadSystemClass(this.methodClass);
        this.LoadSystemClass(this.symbolClass);
        this.LoadSystemClass(this.integerClass);
        this.LoadSystemClass(this.primitiveClass);
        this.LoadSystemClass(this.stringClass);
        this.LoadSystemClass(this.doubleClass);

        // Fix up objectClass
        this.objectClass.SuperClass = this.nilObject;

        // Load the generic block class
        this.blockClass = this.LoadClass(SymbolFor("Block"));

        // Setup the true and false objects
        var trueSymbol = this.SymbolFor("True");
        this.trueClass = this.LoadClass(trueSymbol);
        this.trueObject = this.NewInstance(trueClass);

        var falseSymbol = this.SymbolFor("False");
        this.falseClass = this.LoadClass(falseSymbol);
        this.falseObject = this.NewInstance(this.falseClass);

        // Load the system class and create an instance of it
        this.systemClass = this.LoadClass(this.SymbolFor("System"));
        var systemObject = this.NewInstance(this.systemClass);

        // Put special objects and classes into the dictionary of globals
        this.SetGlobal(this.SymbolFor("nil"), this.nilObject);
        this.SetGlobal(this.SymbolFor("true"), this.trueObject);
        this.SetGlobal(this.SymbolFor("false"), this.falseObject);
        this.SetGlobal(this.SymbolFor("system"), systemObject);
        this.SetGlobal(this.SymbolFor("System"), this.systemClass);
        this.SetGlobal(this.SymbolFor("Block"), this.blockClass);

        this.SetGlobal(trueSymbol, this.trueClass);
        this.SetGlobal(falseSymbol, this.falseClass);
        return systemObject;
    }

    public SSymbol SymbolFor(string text)
    {
        if(!this.symbolTable.TryGetValue(text, out var s))
        {
            this.symbolTable[text] = s = NewSymbol(text);
        }
        return s;
    }
        // Lookup the symbol in the symbol table

    public SArray NewArray(long length) => new (nilObject, length);

    public SArray NewArray<T>(List<T> list) where T : class
    {
        // Allocate a new array with the same length as the list
        var result = NewArray(list.Count);
        // Copy all elements from the list into the array
        for (int i = 0; i < list.Count; i++)
        {
            result.SetIndexableField(i, list[i] as SAbstractObject);
        }
        // Return the allocated and initialized array
        return result;
    }

    public SArray NewArray(string[] stringArray)
    {
        // Allocate a new array with the same length as the string array
        var result = NewArray(stringArray.Length);
        // Copy all elements from the string array into the array
        for (int i = 0; i < stringArray.Length; i++)
        {
            result.SetIndexableField(i, NewString(stringArray[i]));
        }

        // Return the allocated and initialized array
        return result;
    }

    public SBlock NewBlock(SMethod method, Frame context, int arguments) =>
        // Allocate a new block and set its class to be the block class
        new (method, context, GetBlockClass(arguments));

    public SClass NewClass(SClass classClass)
    {
        // Allocate a new class and set its class to be the given class class
        var result = new SClass(classClass.NumberOfInstanceFields, this);
        result.SetClass(classClass);
        // Return the freshly allocated class
        return result;
    }

    public Frame NewFrame(Frame previousFrame, SMethod method, Frame context)
    {
        // Compute the maximum number of stack locations (including arguments,
        // locals and extra buffer to support doesNotUnderstand) and set the number
        // of indexable fields accordingly
        var length = method.NumberOfArguments
            + method.NumberOfLocals
            + method.MaximumNumberOfStackElements + 2;

        return new Frame(nilObject, previousFrame, context, method, length);
    }

    public SMethod NewMethod(SSymbol signature, int numberOfBytecodes,
         int numberOfLocals,
         int maxNumStackElements, List<SAbstractObject> literals) =>
        // Allocate a new method and set its class to be the method class
        new (signature, numberOfBytecodes,
            numberOfLocals, maxNumStackElements, literals);

    public SObject NewInstance(SClass instanceClass)
    {
        // Allocate a new instance and set its class to be the given class
        var result = new SObject(instanceClass.NumberOfInstanceFields,
            nilObject);
        result.SetClass(instanceClass);
        // Return the freshly allocated instance
        return result;
    }

    public SInteger NewInteger(long value) => SInteger.GetInteger(value);

    public SBigInteger NewBigInteger(BigInteger value) => new (value);

    public SDouble NewDouble(double value) => new (value);

    public SClass NewMetaclassClass()
    {
        // Allocate the metaclass classes
        var result = new SClass(this);
        result.SetClass(new SClass(this));
        // Setup the metaclass hierarchy
        result.SOMClass.SetClass(result);
        // Return the freshly allocated metaclass class
        return result;
    }

    public SString NewString(string embeddedString) =>
        // Allocate a new string and set its class to be the string class
        new (embeddedString);

    private SSymbol NewSymbol(string str) =>
        // Allocate a new symbol and set its class to be the symbol class
        // Insert the new symbol into the symbol table
        this.symbolTable[str] = new (str);// Return the freshly allocated symbol

    public SClass NewSystemClass()
    {
        // Allocate the new system class
        var systemClass = new SClass(this);
        // Setup the metaclass hierarchy
        systemClass.SetClass(new SClass(this));
        systemClass.SOMClass.SetClass(metaclassClass);
        // Return the freshly allocated system class
        return systemClass;
    }

    public void InitializeSystemClass(SClass systemClass, SClass superClass,string name)
    {
        // Initialize the superclass hierarchy
        if (superClass != null)
        {
            systemClass.SuperClass = superClass;
            systemClass.SOMClass.SuperClass = superClass.SOMClass;
        }
        else
        {
            systemClass.SOMClass.SuperClass = classClass;
        }

        // Initialize the array of instance fields
        systemClass.InstanceFields = NewArray(0);
        systemClass.SOMClass.InstanceFields = NewArray(0);
        // Initialize the array of instance invokables
        systemClass.InstanceInvokables = NewArray(0);
        systemClass.SOMClass.InstanceInvokables = NewArray(0);
        // Initialize the name of the system class
        systemClass.Name = SymbolFor(name);
        systemClass.SOMClass.Name = SymbolFor(name + " class");
        // Insert the system class into the dictionary of globals
        this.SetGlobal(systemClass.Name, systemClass);
    }

    public SAbstractObject GetGlobal(SSymbol name) =>
        // Return the global with the given name if it's in the dictionary of
        // globals
        this.HasGlobal(name) ? globals[name] : null;

    public void SetGlobal(SSymbol name, SAbstractObject value) =>
        // Insert the given value into the dictionary of globals
        this.globals[name] = value;

    public bool HasGlobal(SSymbol name) =>
        // Returns if the universe has a value for the global of the given name
        this.globals.ContainsKey(name);

    public SClass BlockClass =>
        // Get the generic block class
        this.blockClass;

    public SClass GetBlockClass(int numberOfArguments)
    {
        // Compute the name of the block class with the given number of
        // arguments
        var name = SymbolFor("Block" + (numberOfArguments));
        // Lookup the specific block class in the dictionary of globals and
        // return it
        if (this.HasGlobal(name)) return (SClass)this.GetGlobal(name);
        // Get the block class for blocks with the given number of arguments
        var result = LoadClass(name, null);
        // Add the appropriate value primitive to the block class
        result.AddInstancePrimitive(SBlock.GetEvaluationPrimitive(numberOfArguments, this));
        // Insert the block class into the dictionary of globals
        SetGlobal(name, result);
        // Return the loaded block class
        return result;
    }

    public SClass LoadClass(SSymbol name)
    {
        // Check if the requested class is already in the dictionary of globals
        if (HasGlobal(name)) return (SClass)GetGlobal(name);
        // Load the class
        var result = LoadClass(name, null);
        // Load primitives (if necessary) and return the resulting class
        if (result != null && result.HasPrimitives)
            result.LoadPrimitives();
        this.SetGlobal(name, result);
        return result;
    }

    public void LoadSystemClass(SClass systemClass)
    {
        var result = LoadClass(systemClass.Name, systemClass);
        if (result == null) {
            throw new ProgramDefinitionError("Failed to load the "
                + systemClass.Name.EmbeddedString + " class."
                + " This is unexpected and may indicate that the classpath is not set correctly,"
                + " or that the core library is not available.");
        }

        // Load primitives if necessary
        if (result.HasPrimitives) result.LoadPrimitives();
    }

    private SClass LoadClass(SSymbol name, SClass systemClass)
    {
        // Try loading the class from all different paths
        foreach (string cpEntry in classPaths) {
            var fn = Path.Combine(
                cpEntry,name.EmbeddedString+ ".som");
            if (!File.Exists(fn)) continue;
            try
            {
                // Load the class from a file and return the loaded class
                var result = SourcecodeCompiler.CompileClass(cpEntry,
                    name.EmbeddedString, systemClass, this);
                if (dumpBytecodes)
                {
                    Disassembler.Dump(result.SOMClass, this);
                    Disassembler.Dump(result, this);
                }
                return result;

            }
            catch (IOException)
            {
                // Continue trying different paths
            }
        }

        // The class could not be found.
        return null;
    }

    public SClass LoadShellClass(string stmt)
    {
        // java.io.ByteArrayInputStream in = new
        // java.io.ByteArrayInputStream(stmt.getBytes());
        // Load the class from a stream and return the loaded class
        try {
            var result = SourcecodeCompiler.CompileClass(stmt, null, this);
            if (dumpBytecodes) Disassembler.Dump(result, this);
            return result;
        } catch (ProgramDefinitionError e) {
            ErrorExit(e.ToString());
            throw e;
        }
    }

    public static void ErrorPrint(string msg)
    {
        // Checkstyle: stop
        Console.Error.Write(msg);
        // Checkstyle: resume
    }

    public static void ErrorPrintln(string msg)
    {
        // Checkstyle: stop
        Console.Error.WriteLine(msg);
        // Checkstyle: resume
    }

    public static void ErrorPrintln()
    {
        // Checkstyle: stop
        Console.Error.WriteLine();
        // Checkstyle: resume
    }

    public static void Print(string msg)
    {
        // Checkstyle: stop
        Console.Error.Write(msg);
        // Checkstyle: resume
    }

    public static void Println(string msg)
    {
        // Checkstyle: stop
        Console.Error.WriteLine(msg);
        // Checkstyle: resume
    }

    public static void Println()
    {
        // Checkstyle: stop
        Console.Error.WriteLine();
        // Checkstyle: resume
    }

    public SObject nilObject;
    public SObject trueObject;
    public SObject falseObject;
    public SClass objectClass;
    public SClass classClass;
    public SClass metaclassClass;
    public SClass nilClass;
    public SClass integerClass;
    public SClass arrayClass;
    public SClass methodClass;
    public SClass symbolClass;
    public SClass primitiveClass;
    public SClass stringClass;
    public SClass systemClass;
    public SClass blockClass;
    public SClass doubleClass;
    public SClass trueClass;
    public SClass falseClass;
    protected Dictionary<SSymbol, SAbstractObject> globals = new();
    protected List<string> classPaths =new();
    protected bool dumpBytecodes;
    public readonly static char pathSeparator = ':'; //should be ':'
    public readonly static char fileSeparator = Path.DirectorySeparatorChar;
    protected Interpreter interpreter;
    protected Dictionary<string, SSymbol> symbolTable;
    // TODO: this is not how it is supposed to be... it is just a hack to cope
    // with the use of system.exit in SOM to enable testing
    protected bool avoidExit;
    protected int lastExitCode;
    protected static Universe current;
}
