using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.NT;

public class GStringReader(string text) : StringReader(text), GLocationReader
{
    protected int line = 0;
    protected int column = 0;
    protected int position = 0;
    protected int length = text?.Length ?? 0;
    protected string text = string.Empty;
    public virtual string Text => this.text;
    public virtual int Line => this.line;
    public virtual int Column => this.column;
    public virtual int Position => this.position;
    public virtual int Length => this.length;

    public GLocationReader Clone() 
        => new GStringReader(this.text) 
        {
            length = this.length, 
            column = this.column, 
            line = this.line, 
            position = this.position, 
            text = this.text 
        };
    protected override void Dispose(bool disposing)
    {
        this.line = 0;
        this.column = 0;
        this.position = 0;
        this.length = 0;
        this.text = string.Empty;
        base.Dispose(disposing);
    }
    public override void Close() 
        => this.Dispose(true);
    public override bool Equals(object o)
        => o is GStringReader reader 
        && reader.text == this.text 
        && reader.position == this.position 
        && reader.length == this.length
        && reader.line == this.line 
        && reader.column == this.column;
    public override int GetHashCode() 
        => this.text.GetHashCode() 
        ^ this.position
        ^ this.length
        ^ this.line 
        ^ this.column;
    public override int Peek()
        => this.position < this.length 
        ? this.text[this.position] 
        : -1;
    public override int Read()
        => this.ProcessLine(
            this.position < this.length
            ? this.text[this.position++]
            : -1);

    protected virtual char ProcessLine(char ch)
        => (char)this.ProcessLine((int)ch);
        
    protected virtual int ProcessLine(int ch)
    {
        if (ch < 0) return ch;
        this.position++;
        if (ch == '\r' || ch == '\n')
        {
            this.column = 1;
            this.line++;
            if (ch == '\r' && this.Peek() == '\n')
                if (this.position < this.length)
                    this.position++;
        }
        else
            this.column++;
        return ch;
    }
    public override int Read(char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        else if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        else if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        else if (buffer.Length - index < count)
            throw new ArgumentException("Argument_InvalidOffLen");
        var delta = this.length - this.position;
        if (delta > 0)
        {
            if (delta > count) delta = count;
            for(int i = 0; i < delta; i++)
                buffer[index + i] = this.ProcessLine(this.text[this.position + i]);
            this.position += delta;
        }
        return delta;
    }
    public override int Read(Span<char> buffer)
    {
        if (buffer.IsEmpty)
            throw new ArgumentNullException(nameof(buffer));
        var length = buffer.Length;
        var count = this.length - this.position;
        if (count > 0)
        {
            if (count > length) count = length;
            for(int i = 0; i < count; i++)
                buffer[i] = this.ProcessLine(this.text[i]);
            this.position += count;
        }
        return count;
    }
    public override Task<int> ReadAsync(char[] buffer, int index, int count) => buffer switch
    {
        null => throw new ArgumentNullException(nameof(buffer)),
        _ => index < 0 || count < 0
            ? throw new ArgumentOutOfRangeException(
                index < 0 ? nameof(index) : nameof(count), "ArgumentOutOfRange_NeedNonNegNum")
            : buffer.Length - index < count
            ? throw new ArgumentException("Argument_InvalidOffLen")
            : Task.FromResult(Read(buffer, index, count)),
    };
    public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) 
        => !cancellationToken.IsCancellationRequested
            ? new ValueTask<int>(Read(buffer.Span))
            : ValueTask.FromCanceled<int>(cancellationToken);
    public override int ReadBlock(char[] buffer, int index, int count)
        => this.Read(buffer, index, count);
    public override int ReadBlock(Span<char> buffer) 
        => this.Read(buffer);
    public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) => buffer switch
    {
        null => throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer"),
        _ => index < 0 || count < 0
            ? throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), "ArgumentOutOfRange_NeedNonNegNum")
            : buffer.Length - index < count
            ? throw new ArgumentException("Argument_InvalidOffLen")
            : Task.FromResult(ReadBlock(buffer, index, count)),
    };
    public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default) 
        => !cancellationToken.IsCancellationRequested
            ? new ValueTask<int>(ReadBlock(buffer.Span))
            : ValueTask.FromCanceled<int>(cancellationToken);
    public override string ReadLine()
    {
        if (this.Peek() == -1) return null;
        var builder = new StringBuilder();
        int i = this.position;
        for (; i < this.length; i++)
        {
            var c = this.ProcessLine(text[i]);
            if (c is '\n' or '\r') break;
            builder.Append(c);
        }
        return builder.ToString();
    }
    public override Task<string> ReadLineAsync() 
        => Task.FromResult(ReadLine());
    public override string ReadToEnd()
    {
        var builder = new StringBuilder();
        for(int i = this.position; i < this.length; i++)
            builder.Append(this.ProcessLine(this.text[i]));
        this.position = this.length;
        return builder.ToString();
    }
    public override Task<string> ReadToEndAsync()
        => Task.FromResult(ReadToEnd());
    public override string ToString()
        => $"\"{this.Text[..16]}...\": Line={this.line}, Column={this.column}, Position={this.position}";
}
