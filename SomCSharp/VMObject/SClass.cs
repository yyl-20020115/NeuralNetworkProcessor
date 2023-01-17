/**
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
namespace Som.VMObject;
using Som.Primitives;
using Som.VM;
using System.Reflection;

public class SClass : SObject
{

    protected Universe universe;

    public SClass(Universe universe) : base(universe.nilObject)
    {
        // Initialize this class by calling the super constructor
        this.invokablesTable = new ();
        this.universe = universe;
    }

    public SClass(int numberOfFields, Universe universe)
            : base(numberOfFields, universe.nilObject)
    {
        // Initialize this class by calling the super constructor with the given
        // value

        this.invokablesTable = new ();
        this.universe = universe;
    }

    public SObject SuperClass { get =>
      // Get the super class by reading the field with super class index
      superclass; set =>
                          // Set the super class by writing to the field with super class index
                          superclass = value; }

    public bool HasSuperClass =>
        // Check whether or not this class has a super class
        superclass != universe.nilObject;

    public SSymbol Name { get =>
      // Get the name of this class by reading the field with name index
      name; set =>
                    // Set the name of this class by writing to the field with name index
                    name = value; }

    public SArray InstanceFields { get =>
      // Get the instance fields by reading the field with the instance fields
      // index
      instanceFields; set =>
                              // Set the instance fields by writing to the field with the instance
                              // fields index
                              instanceFields = value; }

    public SArray InstanceInvokables
    {
        get =>
// Get the instance invokables by reading the field with the instance
// invokables index
instanceInvokables;
        set
        {
            // Set the instance invokables by writing to the field with the instance
            // invokables index
            instanceInvokables = value;

            // Make sure this class is the holder of all invokables in the array
            for (int i = 0; i < NumberOfInstanceInvokables; i++)
            {
                GetInstanceInvokable(i).Holder = this;
            }
        }
    }

    public int NumberOfInstanceInvokables =>
        // Return the number of instance invokables in this class
        InstanceInvokables.NumberOfIndexableFields;

    public ISInvokable GetInstanceInvokable(int index) =>
        // Get the instance invokable with the given index
        (ISInvokable)InstanceInvokables.GetIndexableField(index);

    public void SetInstanceInvokable(int index, ISInvokable value)
    {
        // Set this class as the holder of the given invokable
        value.Holder = this;

        // Set the instance method with the given index to the given value
        InstanceInvokables.SetIndexableField(index, (SAbstractObject)value);
    }

    public override int DefaultNumberOfFields =>
        // Return the default number of fields in a class
        numberOfClassFields;

    public ISInvokable LookupInvokable(SSymbol signature)
    {
        // Lookup invokable and return if found
        //var invokable = invokablesTable[signature];
        //if (invokable != null) return invokable;
        if(invokablesTable.TryGetValue(signature, out var invokable)) return invokable; 
        // Lookup invokable with given signature in array of instance invokables
        for (int i = 0; i < NumberOfInstanceInvokables; i++)
        {
            // Get the next invokable in the instance invokable array
            invokable = GetInstanceInvokable(i);

            // Return the invokable if the signature matches
            if (invokable.Signature == signature)
            {
                invokablesTable[signature]= invokable;
                return invokable;
            }
        }

        // Traverse the super class chain by calling lookup on the super class
        if (HasSuperClass)
        {
            invokable = ((SClass)SuperClass).LookupInvokable(signature);
            if (invokable != null)
            {
                invokablesTable[signature]= invokable;
                return invokable;
            }
        }

        // Invokable not found
        return null;
    }

    public int LookupFieldIndex(SSymbol fieldName)
    {
        // Lookup field with given name in array of instance fields
        for (int i = NumberOfInstanceFields - 1; i >= 0; i--)
            // Return the current index if the name matches
            if (fieldName == GetInstanceFieldName(i)) return i;

        // Field not found
        return -1;
    }

    public bool AddInstanceInvokable(ISInvokable value)
    {
        // Add the given invokable to the array of instance invokables
        for (int i = 0, c = NumberOfInstanceInvokables; i < c; i++)
        {
            // Get the next invokable in the instance invokable array
            var invokable = GetInstanceInvokable(i);

            // Replace the invokable with the given one if the signature matches
            if (invokable.Signature == value.Signature)
            {
                SetInstanceInvokable(i, value);
                return false;
            }
        }

        // Append the given method to the array of instance methods
        InstanceInvokables = InstanceInvokables.CopyAndExtendWith(
            (SAbstractObject)value, universe);
        return true;
    }

    //public void AddInstancePrimitive(SPrimitive value) => AddInstancePrimitive(value, false);

    public void AddInstancePrimitive(SPrimitive value, bool suppressWarning=true)
    {
        if (AddInstanceInvokable(value) && !suppressWarning)
        {
            Universe.Print("Warning: Primitive " + value.Signature.EmbeddedString);
            Universe.Println(" is not in class definition for class "
                + Name.EmbeddedString +". It is maybe a primitive.");
        }
    }

    public SSymbol GetInstanceFieldName(int index)
    {
        // Get the name of the instance field with the given index
        if (index >= NumberOfSuperInstanceFields)
        {
            // Adjust the index to account for fields defined in the super class
            index -= NumberOfSuperInstanceFields;

            // Return the symbol representing the instance fields name
            return (SSymbol)InstanceFields.GetIndexableField(index);
        }
        else
        {
            // Ask the super class to return the name of the instance field
            return ((SClass)SuperClass).GetInstanceFieldName(index);
        }
    }

    public int NumberOfInstanceFields =>
        // Get the total number of instance fields in this class
        instanceFields.NumberOfIndexableFields
            + NumberOfSuperInstanceFields;

    private int NumberOfSuperInstanceFields =>
        // Get the total number of instance fields defined in super classes
        HasSuperClass ? ((SClass)SuperClass).NumberOfInstanceFields : 0;

    public bool HasPrimitives
    {
        get
        {
            // Lookup invokable with given signature in array of instance invokables
            for (int i = 0; i < NumberOfInstanceInvokables; i++)
            {
                // Get the next invokable in the instance invokable array
                if (GetInstanceInvokable(i).IsPrimitive) return true;
            }
            return false;
        }
    }

    public void LoadPrimitives()
    {
        // Compute the class name of the Java(TM) class containing the
        // primitives
        var className = "Som.Primitives." + Name.EmbeddedString
            + "Primitives";

        // Try loading the primitives
        try
        {
            var primitivesClass = Type.GetType(className);
            try
            {
                var ctor = primitivesClass.GetConstructor( BindingFlags.Public| BindingFlags.Instance,new Type[] { typeof(Universe) } );
                ((Primitives)ctor.Invoke(new object[] { universe })).InstallPrimitivesIn(this);
            }
            catch (Exception)
            {
                Universe.Println("Primitives class " + className
                    + " cannot be instantiated");
            }
        }
        catch (Exception)
        {
            Universe.Println("Primitives class " + className + " not found");
        }
    }

    public override string ToString() => "Class(" + Name.EmbeddedString + ")";

    // Implementation specific fields
    protected SObject superclass;
    protected SSymbol name;
    protected SArray instanceInvokables;
    protected SArray instanceFields;

    // Mapping of symbols to invokables
    protected Dictionary<SSymbol, ISInvokable> invokablesTable;

    // Static field indices and number of class fields
    protected static int numberOfClassFields = numberOfObjectFields;
}
