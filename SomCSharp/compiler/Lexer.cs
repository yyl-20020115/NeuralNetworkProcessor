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
using System.Text;

public class Lexer
{
    public static string SEPARATOR = "----";
    public static string PRIMITIVE = "primitive";

    protected int lineNumber;
    protected int charsRead; // all characters read, excluding the current line
    protected TextReader reader;
    protected Symbol sym;
    protected char symc;
    protected StringBuilder builder;
    protected bool peekDone;
    protected Symbol nextSym;
    protected char nextSymc;
    protected StringBuilder nextText;
    protected string buffer;
    protected int bufferPosition;

    public Lexer(TextReader reader)
    {
        this.reader = reader;
        this.peekDone = false;
        this.buffer = "";
        this.builder = new ();
        this.bufferPosition = 0;
        this.lineNumber = 0;
    }

    public bool PeekDone => peekDone;

    public Symbol GetSymbol()
    {
        if (this.peekDone)
        {
            this.peekDone = false;
            this.sym = nextSym;
            this.symc = nextSymc;
            this.builder = new (nextText.ToString());
            return this.sym;
        }

        do
        {
            if (!this.HasMoreInput())
            {
                this.sym = Symbol.NONE;
                this.symc = '\0';
                this.builder = new (this.symc);
                return sym;
            }
            SkipWhiteSpace();
            SkipComment();
        } while (IsEndOfBuffer|| char.IsWhiteSpace(CurrentChar) || CurrentChar== '"');

        if (this.CurrentChar== '\'')
        {
            this.GetLexString();
        }
        else if (this.CurrentChar== '[')
        {
            this.Match(Symbol.NewBlock);
        }
        else if (this.CurrentChar== ']')
        {
            this.Match(Symbol.EndBlock);
        }
        else if (this.CurrentChar== ':')
        {
            if (this.GetBufferChar(bufferPosition + 1) == '=')
            {
                this.bufferPosition += 2;
                this.sym = Symbol.Assign;
                this.symc = '\0';
                this.builder = new (":=");
            }
            else
            {
                this.bufferPosition++;
                this.sym = Symbol.Colon;
                this.symc = ':';
                this.builder = new (":");
            }
        }
        else if (this.CurrentChar== '(')
        {
            this.Match(Symbol.NewTerm);
        }
        else if (this.CurrentChar== ')')
        {
            this.Match(Symbol.EndTerm);
        }
        else if (this.CurrentChar== '#')
        {
            this.Match(Symbol.Pound);
        }
        else if (this.CurrentChar== '^')
        {
            this.Match(Symbol.Exit);
        }
        else if (this.CurrentChar== '.')
        {
            this.Match(Symbol.Period);
        }
        else if (this.CurrentChar== '-')
        {
            if (this.buffer[bufferPosition..].StartsWith(SEPARATOR))
            {
                this.builder = new ();
                while(this.CurrentChar== '-')
                {
                    this.builder.Append(GetBufferChar(bufferPosition++));
                }
                this.sym = Symbol.Separator;
            }
            else
            {
                this.GetLexOperator();
            }
        }
        else if (IsOperator(this.CurrentChar))
        {
            this.GetLexOperator();
        }
        else if (this.buffer[bufferPosition..].StartsWith(PRIMITIVE))
        {
            this.bufferPosition += PRIMITIVE.Length;
            this.sym = Symbol.Primitive;
            this.symc = '\0';
            this.builder = new (PRIMITIVE);
        }
        else if (char.IsLetter(CurrentChar))
        {
            this.symc = '\0';
            this.builder = new ();
            while (char.IsLetterOrDigit(this.CurrentChar) || this.CurrentChar== '_')
            {
                this.builder.Append(GetBufferChar(this.bufferPosition++));
            }
            this.sym = Symbol.Identifier;
            if (GetBufferChar(this.bufferPosition) == ':')
            {
                this.sym = Symbol.Keyword;
                this.bufferPosition++;
                this.builder.Append(':');
                if (char.IsLetter(CurrentChar))
                {
                    this.sym = Symbol.KeywordSequence;
                    while (char.IsLetter(this.CurrentChar) || this.CurrentChar== ':')
                    {
                        this.builder.Append(GetBufferChar(bufferPosition++));
                    }
                }
            }
        }
        else if (char.IsDigit(this.CurrentChar))
        {
            this.GetLexNumber();
        }
        else
        {
            this.sym = Symbol.NONE;
            this.symc = CurrentChar;
            this.builder = new (symc);
        }

        return this.sym;
    }

    private void GetLexNumber()
    {
        this.sym = Symbol.Integer;
        this.symc = '\0';
        this.builder = new ();

        bool sawDecimalMark = false;

        do
        {
            this.builder.Append(GetBufferChar(this.bufferPosition++));
            if (!sawDecimalMark &&
                '.' == this.CurrentChar&&
                char.IsDigit(GetBufferChar(this.bufferPosition + 1)))
            {
                this.sym = Symbol.Double;
                this.builder.Append(GetBufferChar(this.bufferPosition++));
            }
        } while (char.IsDigit(this.CurrentChar));
    }

    private void GetLexEscapeChar()
    {
        //assert!endOfBuffer();
        var current = this.CurrentChar;
        switch (current)
        {
            // @formatter:off
            case 't': this.builder.Append('\t'); break;
            case 'b': this.builder.Append('\b'); break;
            case 'n': this.builder.Append('\n'); break;
            case 'r': this.builder.Append('\r'); break;
            case 'f': this.builder.Append('\f'); break;
            case '\'': this.builder.Append('\''); break;
            case '\\': this.builder.Append('\\'); break;
            case '0': this.builder.Append('\0'); break;
                // @formatter:on
        }
        this.bufferPosition++;
    }

    private void GetLexStringChar()
    {
        if (this.CurrentChar== '\\')
        {
            this.bufferPosition++;
            this.GetLexEscapeChar();
        }
        else
        {
            this.builder.Append(this.CurrentChar);
            this.bufferPosition++;
        }
    }

    private void GetLexString()
    {
        this.sym = Symbol.STString;
        this.symc = '\0';
        this.builder = new ();
        this.bufferPosition++;

        while (this.CurrentChar!= '\'')
        {
            while (this.IsEndOfBuffer)
            {
                if (this.FillBuffer() == -1) return;
                this.builder.Append('\n');
            }
            if (this.CurrentChar!= '\'')
            {
                this.GetLexStringChar();
            }
        }

        this.bufferPosition++;
    }

    private void GetLexOperator()
    {
        if (IsOperator(GetBufferChar(this.bufferPosition + 1)))
        {
            this.sym = Symbol.OperatorSequence;
            this.symc = '\0';
            this.builder = new ();
            while (IsOperator(this.CurrentChar))
            {
                this.builder.Append(GetBufferChar(this.bufferPosition++));
            }
        }
        else if (this.CurrentChar== '~')
        {
            Match(Symbol.Not);
        }
        else if (this.CurrentChar== '&')
        {
            Match(Symbol.And);
        }
        else if (this.CurrentChar== '|')
        {
            Match(Symbol.Or);
        }
        else if (this.CurrentChar== '*')
        {
            Match(Symbol.Star);
        }
        else if (this.CurrentChar== '/')
        {
            Match(Symbol.Div);
        }
        else if (this.CurrentChar== '\\')
        {
            Match(Symbol.Mod);
        }
        else if (this.CurrentChar== '+')
        {
            Match(Symbol.Plus);
        }
        else if (this.CurrentChar== '=')
        {
            Match(Symbol.Equal);
        }
        else if (this.CurrentChar== '>')
        {
            Match(Symbol.More);
        }
        else if (this.CurrentChar== '<')
        {
            Match(Symbol.Less);
        }
        else if (this.CurrentChar== ',')
        {
            Match(Symbol.Comma);
        }
        else if (this.CurrentChar== '@')
        {
            Match(Symbol.At);
        }
        else if (this.CurrentChar== '%')
        {
            Match(Symbol.Per);
        }
        else if (this.CurrentChar== '-')
        {
            Match(Symbol.Minus);
        }
    }
    public Symbol Peek()
    {
        var saveSym = sym;
        var saveSymc = symc;
        var saveText = new StringBuilder(this.builder.ToString());
        if (peekDone) throw new IllegalStateException("SOM lexer: cannot peek twice!");

        GetSymbol();
        nextSym = sym;
        nextSymc = symc;
        nextText = new (builder.ToString());
        sym = saveSym;
        symc = saveSymc;
        builder = saveText;
        peekDone = true;
        return nextSym;
    }
    public string Text => this.builder.ToString();

    public string NextText => this.nextText.ToString();

    public string RawBuffer => this.buffer;

    public int CurrentLineNumber => this.lineNumber;

    public int CurrentColumn => this.bufferPosition + 1;

    // All characters read and processed, including current line
    protected int NumberOfCharactersRead => this.charsRead + this.bufferPosition;

    private int FillBuffer()
    {
        try
        {
            if (reader.Peek() == -1) return -1;
            charsRead += buffer.Length;
            buffer = reader.ReadLine();
            if (buffer == null) return -1;
            ++lineNumber;
            bufferPosition = 0;
            return buffer.Length;
        }
        catch (IOException ioe)
        {
            throw new IllegalStateException("Error reading from input: " + ioe.ToString());
        }
    }

    private bool HasMoreInput()
    {
        while (IsEndOfBuffer) if (FillBuffer() == -1) return false;
        return true;
    }

    private void SkipWhiteSpace()
    {
        while (char.IsWhiteSpace(CurrentChar))
        {
            bufferPosition++;
            while (IsEndOfBuffer) if (FillBuffer() == -1) return;
        }
    }

    private void SkipComment()
    {
        if (this.CurrentChar== '"')
        {
            do
            {
                this.bufferPosition++;
                while (this.IsEndOfBuffer)
                    if (this.FillBuffer() == -1) return;
            } while (this.CurrentChar!= '"');
            bufferPosition++;
        }
    }

    private char CurrentChar => GetBufferChar(this.bufferPosition);

    private bool IsEndOfBuffer => this.bufferPosition >= this.buffer.Length;

    public static bool IsOperator(char c) =>
        c == '~' || c == '&' || c == '|' || c == '*' || c == '/'
            || c == '\\' || c == '+' || c == '=' || c == '>' || c == '<'
            || c == ',' || c == '@' || c == '%' || c == '-';

    private void Match(Symbol s)
    {
        this.sym = s;
        this.symc = CurrentChar;
        this.builder = new (symc.ToString());
        this.bufferPosition++;
    }

    private char GetBufferChar(int p) => p >= buffer.Length ? '\0' : buffer[p];
}