using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.NT;

public class GStreamReader : StreamReader, GLocationReader
{
    public GStreamReader(Stream stream) 
        : base(stream) { }
    public GStreamReader(string path) 
        : base(path) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
        : base(stream, detectEncodingFromByteOrderMarks) { }
    public GStreamReader(Stream stream, Encoding encoding)
        : base(stream, encoding) { }
    public GStreamReader(string path, FileStreamOptions options) 
        : base(path, options) { this.path = path; this.stream = base.BaseStream; this.options = options; }
    public GStreamReader(string path, bool detectEncodingFromByteOrderMarks) 
        : base(path, detectEncodingFromByteOrderMarks) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(string path, Encoding encoding) 
        : base(path, encoding) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks) 
        : base(stream, encoding, detectEncodingFromByteOrderMarks) { }
    public GStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks) 
        : base(path, encoding, detectEncodingFromByteOrderMarks) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        : base(stream, encoding, detectEncodingFromByteOrderMarks) { }
    public GStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        : base(path, encoding, detectEncodingFromByteOrderMarks) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
        : base(path, encoding, detectEncodingFromByteOrderMarks) { this.path = path; this.stream = base.BaseStream; }
    public GStreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
        : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen) { }

    public static Stream? CloneStream(Stream stream, int bufferSize = -1, FileOptions options = FileOptions.None) => stream switch
    {
        FileStream fileStream => new FileStream(fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, options),
        MemoryStream memoryStream => new MemoryStream(memoryStream.ToArray()) { Position = memoryStream.Position },
        _ => null,
    };
    public GLocationReader Clone()
        => this.options != null && File.Exists(path)
        ? new GStreamReader(path, options)
        { 
            detectEncodingFromByteOrderMarks=this.detectEncodingFromByteOrderMarks,
            bufferSize=this.bufferSize,
            leaveOpen=this.leaveOpen,
            line=this.line,
            column=this.column,
            position = this.position,
            options = this.options,
        }
        : stream != null
            ? new GStreamReader(
                CloneStream(this.stream,this.bufferSize),
                this.encoding,
                !this.detectEncodingFromByteOrderMarks.HasValue | this.detectEncodingFromByteOrderMarks.Value,
                this.bufferSize,
                this.leaveOpen.HasValue && this.leaveOpen.Value)
            {
                detectEncodingFromByteOrderMarks = this.detectEncodingFromByteOrderMarks,
                bufferSize = this.bufferSize,
                leaveOpen = this.leaveOpen,
                line = this.line,
                column = this.column,
                position = this.position,
                options = this.options,
            }
            : (GLocationReader)null;
    public override int GetHashCode()
        => this.stream!.GetHashCode()
        ^ this.position
        ^ this.Length
        ^ this.line
        ^ this.column
        ;

    protected string path = "";
    protected Stream? stream = null;
    protected bool? detectEncodingFromByteOrderMarks = null;
    protected Encoding? encoding = null;
    protected int bufferSize = -1;
    protected bool? leaveOpen = null;
    protected FileStreamOptions? options = null;
    public virtual int Line => this.line;
    public virtual int Column => this.column;
    public virtual int Position => this.position;
    public virtual int Length => (int)this.BaseStream.Length;

    protected int line = 0;
    protected int column = 0;
    protected int position = 0;
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.line = 0;
            this.column = 0;
            this.position = 0;
        }
        base.Dispose(disposing);
    }
    public override bool Equals(object? o)
        => o is GStreamReader reader
        && reader.BaseStream == this.BaseStream
        && reader.position == this.position
        && reader.line == this.line
        && reader.column == this.column
        ;

    public override int Read() 
        => this.ProcessLine(base.Read());
    protected virtual char ProcessLine(char c) 
        => (char)this.ProcessLine((int)c);
    protected virtual int ProcessLine(int c)
    {
        if (c < 0) return c;
        this.position++;
        if (c == '\r' || c == '\n')
        {
            this.column = 1;
            this.line++;
            if (c == '\r' && this.Peek() == '\n')
                if (this.position < this.Length)
                    this.position++;
        }
        else
        {
            this.column++;
        }
        return c;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer), ("ArgumentNull_Buffer"));
        else if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), ("ArgumentOutOfRange_NeedNonNegNum"));
        else if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), ("ArgumentOutOfRange_NeedNonNegNum"));
        else if (buffer.Length - index < count)
            throw new ArgumentException(("Argument_InvalidOffLen"));
        var c = this.Length - this.position;
        if (c > 0)
        {
            if (c > count)
            {
                c = count;
            }
            for (int i = 0; i < c; i++)
            {
                int d = this.Read();
                if (d == -1) break;
                buffer[index + i] = this.ProcessLine((char)d);
            }
            this.position += c;
        }
        return c;
    }
    public override int Read(Span<char> buffer)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        var length = buffer.Length;
        var count = this.Length - this.position;
        if (count > 0)
        {
            if (count > length) count = length;
            for (int i = 0; i < count; i++)
            {
                if (this.Read() is int ch && ch == -1) break;
                buffer[i] = this.ProcessLine((char)ch);
            }
            this.position += count;
        }
        return count;
    }
    public override Task<int> ReadAsync(char[] buffer, int index, int count) => buffer switch
    {
        null => throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer"),
        _ => index < 0 || count < 0
                            ? throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", "ArgumentOutOfRange_NeedNonNegNum")
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
    public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) 
        => buffer switch {
        null => throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer"),
        _ => index < 0 || count < 0
                            ? throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", "ArgumentOutOfRange_NeedNonNegNum")
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
        for (; i < this.Length; i++)
        {
            int read = this.Read();
            if (read == -1) break;
            var @char = this.ProcessLine((char)read);
            if (@char == '\n' || @char == '\r') break;
            builder.Append(@char);
        }
        return builder.ToString();
    }
    public override Task<string> ReadLineAsync() 
        => Task.FromResult(ReadLine());
    public override string ReadToEnd()
    {
        var builder = new StringBuilder();
        for (int i = this.position; i < this.Length; i++)
        {
            int read = this.Read();
            if (read == -1) break;
            builder.Append(this.ProcessLine((char)read));
        }
        this.position = this.Length;
        return builder.ToString();
    }
    public override Task<string> ReadToEndAsync() 
        => Task.FromResult(ReadToEnd());
    public override string ToString()
        => $"\"{nameof(GStreamReader)}...\": Line={this.line}, Column={this.column}, Position={this.position}";
}
