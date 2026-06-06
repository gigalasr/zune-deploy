using System.Text;

public class Spinner {
    private static bool ShouldUseAsciiFrames =
        Environment.OSVersion.Platform == PlatformID.Win32NT
                && Console.OutputEncoding.CodePage != 1200 /* UTF-16 */
                && Console.OutputEncoding.CodePage != 65001 /* UTF-8 */;

    private static readonly string[] _framesUnicode = ["◜", "◠", "◝", "◞", "◡", "◟"];
    private static readonly string[] _framesAscii = ["-", "\\", "|", "/"];
    private static readonly string[] _frames = ShouldUseAsciiFrames ? _framesAscii : _framesUnicode;
    private static readonly string _successSymbol = ShouldUseAsciiFrames ? "[OK]" : "✓";
    private static readonly string _failureSymbol = ShouldUseAsciiFrames ? "[FAIL]" : "🞬";

    private object _lock = new();
    private int _spinnerRow;
    private int _frame = 0;
    private int _lastLenght = 0;
    private string _label;

    private TextWriter _originalOut;
    private TextWriter _newOut;

    private Task? _spinnerTask;
    private CancellationTokenSource _cts = new();


    public Spinner() {
        _label = "Loading...";
        _spinnerRow = Console.CursorTop;
        _originalOut = Console.Out;
        _newOut = new Writer(Console.Out.Encoding, this);
    }

    public void Start(string label) {
        _label = label;
        _spinnerRow = Console.CursorTop;
        _cts = new CancellationTokenSource();
        Console.SetOut(_newOut);
        Task.Run(() => { Spin(_cts.Token); });
    }

    public void Stop(string finalLabel, bool faulted = false) {
        _cts.Cancel();
        _spinnerTask?.Wait();
        lock (_lock) {
            Console.SetOut(_originalOut);
            Console.SetCursorPosition(0, _spinnerRow);
            var symbol = faulted ? _failureSymbol : _successSymbol;
            Console.WriteLine($"{symbol} {finalLabel.PadRight(_lastLenght)}");
        }
    }

    public void SetLabel(string label) {
        if (_cts.IsCancellationRequested) {
            return;
        }
        lock (_lock) { _label = label; }
    }

    public void Log(string line) {
        lock (_lock) {
            Console.SetCursorPosition(0, _spinnerRow);
            _originalOut.WriteLine($"{line.PadRight(_lastLenght)}");
            _spinnerRow++;
            DrawSpinner();
        }
    }

    private void Spin(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            lock (_lock) { DrawSpinner(); }
            Thread.Sleep(80);
        }
    }

    private void DrawSpinner() {
        Console.SetCursorPosition(0, _spinnerRow);
        var line = $"{_frames[_frame]} {_label}".PadRight(_lastLenght);
        _originalOut.Write(line);
        _lastLenght = line.Length;
        _frame = (_frame + 1) % _frames.Length;
    }
}

public class Writer : TextWriter {
    public override Encoding Encoding => _encoding;
    private readonly StringBuilder _buffer = new();
    private readonly Encoding _encoding;
    private readonly Spinner _spinner;

    public Writer(Encoding encoding, Spinner spinner) {
        _encoding = encoding;
        _spinner = spinner;
    }

    public override void Write(char value) {
        _buffer.Append(value);
        if (value == '\n') {
            Flush();
        }
    }

    public override void WriteLine(string? line) {
        if (line != null) { _buffer.Append(line); }
        Flush();
    }

    public override void Flush() {
        _spinner.Log(_buffer.ToString());
        _buffer.Clear();
    }
}