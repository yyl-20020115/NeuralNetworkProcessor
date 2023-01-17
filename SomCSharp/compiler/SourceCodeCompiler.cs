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

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;

public class SourcecodeCompiler
{
    protected Parser parser;
    public static SClass CompileClass(string path, string file,
        SClass systemClass, Universe universe) 
        => new SourcecodeCompiler().Compile(path, file, systemClass, universe);
    public static SClass CompileClass(string stmt, SClass systemClass, Universe universe) 
        => new SourcecodeCompiler().CompileClassString(stmt, systemClass, universe);
    private SClass Compile(string path, string file,SClass systemClass, Universe universe)
    {
        var fname = path + Universe.fileSeparator.ToString() + file + ".som";
        this.parser = new (new StreamReader(fname), universe, fname);
        var result = Compile(systemClass);
        var cname = result.Name;
        var cnameC = cname.EmbeddedString;
        if (file != cnameC)
            throw new ProgramDefinitionError("File name " + fname
                + " does not match class name (" + cnameC + ") in it.");
        return result;
    }

    private SClass CompileClassString(string stream,SClass systemClass, Universe universe)
    {
        this.parser = new(new StringReader(stream), universe, "$string$");
        return this.Compile(systemClass);
    }

    private SClass Compile(SClass systemClass)
    {
        var cgc = this.parser.Classdef();
        return systemClass == null ? cgc.Assemble() : cgc.AssembleSystemClass(systemClass);
    }
}
