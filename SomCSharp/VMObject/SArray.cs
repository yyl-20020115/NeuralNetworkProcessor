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
using Som.VM;

public class SArray : SAbstractObject
{
    public SArray(SObject nilObject, long numElements)
    {
        indexableFields = new SAbstractObject[(int)numElements];

        // Clear each and every field by putting nil into them
        for (int i = 0; i < NumberOfIndexableFields; i++)
        {
            SetIndexableField(i, nilObject);
        }
    }

    public SAbstractObject GetIndexableField(long index)
    {
        int i = (int)index;
        if (i >= 0 && i < indexableFields.Length)
        {
            return indexableFields[i];
        }
        else
        {
            return null;
        }
    }

    public void SetIndexableField(long index, SAbstractObject value)
    {
        int i = (int)index;
        if(i>=0 && i< indexableFields.Length)
        {
            indexableFields[i] = value;
        }
        else
        {

        }
    }
    public int NumberOfIndexableFields => indexableFields.Length;

    public SArray CopyAndExtendWith(SAbstractObject value, Universe universe)
    {
        // Allocate a new array which has one indexable field more than this
        // array
        var result = universe.NewArray(NumberOfIndexableFields + 1);

        // Copy the indexable fields from this array to the new array
        CopyIndexableFieldsTo(result);

        // Insert the given object as the last indexable field in the new array
        result.SetIndexableField(NumberOfIndexableFields, value);

        return result;
    }

    protected void CopyIndexableFieldsTo(SArray destination)
    {
        // Copy all indexable fields from this array to the destination array
        for (int i = 0; i < NumberOfIndexableFields; i++)
        {
            destination.SetIndexableField(i, GetIndexableField(i));
        }
    }

    public override SClass GetSOMClass(Universe universe) => universe.arrayClass;

    // Private array of indexable fields
    private readonly SAbstractObject[] indexableFields;
}
