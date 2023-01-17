namespace Som.VMObject;
using Som.VM;

public class SObject : SAbstractObject
{

    public SObject(SObject nilObject)
    {
        fields = new SAbstractObject[DefaultNumberOfFields];
        // Clear each and every field by putting nil into them
        for (int i = 0; i < NumberOfFields; i++)
            SetField(i, nilObject);
    }

    public SObject(int numberOfFields, SObject nilObject)
    {
        fields = new SAbstractObject[numberOfFields];
        // Clear each and every field by putting nil into them
        for (int i = 0; i < NumberOfFields; i++)
            SetField(i, nilObject);
    }

    public virtual SClass SOMClass => clazz;

    public void SetClass(SClass value) =>
        // Set the class of this object by writing to the field with class index
        clazz = value;

    public SSymbol GetFieldName(int index) =>
        // Get the name of the field with the given index
        SOMClass.GetInstanceFieldName(index);

    public int GetFieldIndex(SSymbol name) =>
        // Get the index for the field with the given name
        SOMClass.LookupFieldIndex(name);

    public int NumberOfFields =>
        // Get the number of fields in this object
        fields.Length;

    public virtual int DefaultNumberOfFields =>
        // Return the default number of fields in an object
        numberOfObjectFields;

    public SAbstractObject GetField(long index) =>
        // Get the field with the given index
        fields[(int)index];

    public void SetField(long index, SAbstractObject value) =>
        // Set the field with the given index to the given value
        fields[(int)index] = value;

    public override SClass GetSOMClass(Universe universe) => clazz;

    public override string ToString()
    {
        if (clazz.Name.EmbeddedString == ("SObject"))
        {
            if (fields[1] is SObject)
            {
                var somClazz = (SObject)fields[1];
                var nameSymbolObj = (SObject)somClazz.fields[4];
                var nameString = (SString)nameSymbolObj.fields[0];
                return "SomSom: a " + nameString.EmbeddedString;
            }
        }
        return "a " + GetSOMClass(Universe.Current).Name.EmbeddedString;
    }

    // Private array of fields
    protected SAbstractObject[] fields;
    protected SClass clazz;

    // Static field indices and number of object fields
    public static int numberOfObjectFields = 0;
}
