using System;
using System.Collections.Generic;
using System.IO;

namespace CLanguage;

public class Report (Report.Printer? printer = null)
{
    private readonly int _reportingDisabled = 0;
    private readonly List<string> _extraInformation = [];
    private readonly Printer _printer = printer ?? new Printer ();
    private readonly Dictionary<AbstractMessage, bool> _previousErrors = [];

    public IEnumerable<AbstractMessage> Errors => _previousErrors.Keys;

    public void Error (int code, Syntax.Location loc, Syntax.Location endLoc, string error)
    {
        if (_reportingDisabled > 0)
            return;

        var msg = new ErrorMessage (code, loc, endLoc, error, _extraInformation);
        _extraInformation.Clear ();

        if (!_previousErrors.ContainsKey (msg)) {
            _previousErrors[msg] = true;
            _printer.Print (msg);
        }
    }

    public void Warning (int code, Syntax.Location loc, Syntax.Location endLoc, string warning)
    {
        if (_reportingDisabled > 0)
            return;

        var msg = new WarningMessage (code, loc, endLoc, warning, _extraInformation);
        _extraInformation.Clear ();

        if (!_previousErrors.ContainsKey (msg)) {
            _previousErrors[msg] = true;
            _printer.Print (msg);
        }
    }

    public void Error (int code, Syntax.Location loc, Syntax.Location endLoc, string format, params object[] args)
        => Error (code, loc, endLoc, string.Format (format, args));

    public void Error (int code, string error)
        => Error (code, Syntax.Location.Null, Syntax.Location.Null, error);

    public void Error (int code, string format, params object[] args)
        => Error (code, Syntax.Location.Null, Syntax.Location.Null, String.Format (format, args));

    public void ErrorCode (int code, Syntax.Location loc, Syntax.Location endLoc, params object[] args)
    {
        var m = "";
        switch (code) {
            case 103:
                m = "{0} '{1}' not found";
                break;
            default:
                Error (code, loc, endLoc, string.Join (", ", args));
                return;
        }
        Error (code, loc, endLoc, m, args);
    }

    public void ErrorCode (int code, params object[] args) 
        => ErrorCode (code, Syntax.Location.Null, Syntax.Location.Null, args);

    public class AbstractMessage
    {
        public string MessageType { get; protected set; } = "Info";
        public Syntax.Location Location { get; protected set; }
        public Syntax.Location EndLocation { get; protected set; }
        public bool IsWarning { get; protected set; }
        public bool IsError => !IsWarning;
        public int Code { get; protected set; }
        public string Text { get; protected set; } = "";

        public AbstractMessage (string type, string text)
        {
            MessageType = type;
            Text = text;
        }

        protected AbstractMessage ()
        {
        }
        
        public override bool Equals (object? obj) => (obj is AbstractMessage o) &&
                (o.Code == Code) &&
                (o.Location == Location) &&
                (o.IsWarning == IsWarning) &&
                (o.Text == Text);

        public override int GetHashCode () 
            => Code.GetHashCode () + Location.GetHashCode () + IsWarning.GetHashCode () + Text.GetHashCode ();

        public override string ToString () => Location.IsNull
                ? $"{MessageType.ToLowerInvariant ()} C{Code:0000}: {Text}"
                : $"{Location.Document.Path}({Location.Line},{Location.Column},{EndLocation.Line},{EndLocation.Column}): {MessageType.ToLowerInvariant ()} C{Code:0000}: {Text}";
    }

    public class ErrorMessage : AbstractMessage
    {
        public ErrorMessage (int code, Syntax.Location loc, Syntax.Location endLoc, string error, List<string> extraInformation)
        {
            MessageType = "Error";
            Code = code;
            Text = error;
            Location = loc;
            EndLocation = endLoc;
            IsWarning = false;
        }
    }

    public class WarningMessage : AbstractMessage
    {
        public WarningMessage (int code, Syntax.Location loc, Syntax.Location endLoc, string error, List<string> extraInformation)
        {
            MessageType = "Warning";
            Code = code;
            Text = error;
            Location = loc;
            EndLocation = endLoc;
            IsWarning = true;
        }
    }

    public class Printer
    {
        int warnings, errors;

        public int WarningsCount => warnings;

        public int ErrorsCount => errors;

        public virtual void Print (AbstractMessage msg)
        {
            if (msg.IsWarning)
                ++warnings;
            else
                ++errors;
        }
    }

    public class SavedPrinter : Printer
    {
        public readonly List<AbstractMessage> Messages = [];

        public override void Print (AbstractMessage msg)
        {
            base.Print (msg);

            Messages.Add (msg);
        }
    }

    public class TextWriterPrinter (TextWriter output) : Printer
    {
        private readonly TextWriter output = output;

        public override void Print (AbstractMessage msg)
        {
            base.Print (msg);

            if (!msg.Location.IsNull) {
                output.Write (msg.Location.ToString());
                output.Write (" ");
            }

            output.WriteLine ($"{msg.MessageType} C{msg.Code:0000}: {msg.Text}");
        }
    }
}
