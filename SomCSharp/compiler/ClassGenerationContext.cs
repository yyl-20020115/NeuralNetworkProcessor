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

public class ClassGenerationContext
{
    protected Universe universe;
    protected SSymbol name;
    protected SSymbol superName;
    protected bool classSide;
    protected List<SSymbol> instanceFields = new ();
    protected List<ISInvokable> instanceMethods = new ();
    protected List<SSymbol> classFields = new ();
    protected List<ISInvokable> classMethods = new ();
    public SSymbol Name { get => name; set => this.name = value; }
    public ClassGenerationContext(Universe universe)
    {
        this.universe = universe ?? throw new ArgumentNullException(nameof(universe));
    }

    public void SetSuperName(SSymbol superName) => this.superName = superName;
    public void SetInstanceFieldsOfSuper(SArray fieldNames)
    {
        int numFields = fieldNames.NumberOfIndexableFields;
        for (int i = 0; i < numFields; i++)
            instanceFields.Add(fieldNames.GetIndexableField(i) as SSymbol);
    }
    public void SetClassFieldsOfSuper(SArray fieldNames)
    {
        int numFields = fieldNames.NumberOfIndexableFields;
        for (int i = 0; i < numFields; i++)
            classFields.Add(fieldNames.GetIndexableField(i) as SSymbol);
    }
    public void AddMethod(ISInvokable meth)
    {
        if (classSide)
            classMethods.Add(meth);
        else
            instanceMethods.Add(meth);
    }
    public void StartClassSide() => classSide = true;
    public void AddField(SSymbol field)
    {
        if (classSide)
            classFields.Add(field);
        else
            instanceFields.Add(field);
    }
    public bool HasField(SSymbol field) => (IsClassSide? classFields : instanceFields).Contains(field);
    public byte GetFieldIndex(SSymbol field) => IsClassSide? (byte)classFields.IndexOf(field) : (byte)instanceFields.IndexOf(field);
    public bool IsClassSide => classSide;
    public SClass Assemble()
    {
        // build class class name
        var ccname = name.EmbeddedString + " class";
        // Load the super class
        var superClass = universe.LoadClass(superName);
        // Allocate the class of the resulting class
        var resultClass = universe.NewClass(universe.metaclassClass);
        // Initialize the class of the resulting class
        resultClass.InstanceFields = universe.NewArray(classFields);
        resultClass.InstanceInvokables = universe.NewArray(classMethods);
        resultClass.Name = universe.SymbolFor(ccname);
        var superMClass = superClass.SOMClass;
        resultClass.SuperClass = superMClass;
        // Allocate the resulting class
        var result = universe.NewClass(resultClass);
        // Initialize the resulting class
        result.Name = name;
        result.SuperClass = superClass;
        result.InstanceFields = universe.NewArray(instanceFields);
        result.InstanceInvokables = universe.NewArray(instanceMethods);
        return result;
    }
    public SClass AssembleSystemClass(SClass systemClass)
    {
        systemClass.InstanceInvokables = universe.NewArray(instanceMethods);
        systemClass.InstanceFields = universe.NewArray(instanceFields);
        var superMClass = systemClass.SOMClass;
        superMClass.InstanceInvokables = universe.NewArray(classMethods);
        superMClass.InstanceFields = universe.NewArray(classFields);
        return systemClass;
    }
}
