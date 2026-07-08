using System.Globalization;
using System.Text;

namespace PacketLossTester;

/// <summary>
/// Простой потокобезопасный логгер: пишет события в консоль и в CSV-файл.
/// CSV удобно потом открыть в Excel и посчитать долю потерянных/неотправленных пакетов.
/// </summary>
public sealed class Logger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _lock = new();

    public Logger(string path)
    {
        bool isNewFile = !File.Exists(path);

        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8)
        {
            AutoFlush = true
        };

        if (isNewFile)
        {
            _writer.WriteLine("Timestamp;Seq;Protocol;Destination;Status;SizeBytes;Message");
        }
    }

    public void LogSent(long seq, string protocol, string destination, int sizeBytes)
        => Write(seq, protocol, destination, "SENT", sizeBytes, "");

    public void LogError(long seq, string protocol, string destination, string message, int sizeBytes = 0)
        => Write(seq, protocol, destination, "ERROR", sizeBytes, message);

    public void LogInfo(string message)
    {
        lock (_lock)
        {
            string line = $"{Timestamp()} | INFO | {message}";
            Console.WriteLine(line);
            _writer.WriteLine($"{Timestamp("O")};;;;INFO;;{Escape(message)}");
        }
    }

    private void Write(long seq, string protocol, string destination, string status, int sizeBytes, string message)
    {
        lock (_lock)
        {
            string consoleLine = $"{Timestamp()} | #{seq,-8} | {protocol,-4} | {destination,-21} | {status,-5} | {sizeBytes,5} B {(string.IsNullOrEmpty(message) ? "" : "| " + message)}";
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = status == "ERROR" ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(consoleLine);
            Console.ForegroundColor = prevColor;

            _writer.WriteLine($"{Timestamp("O")};{seq};{protocol};{destination};{status};{sizeBytes};{Escape(message)}");
        }
    }

    private static string Timestamp(string format = "yyyy-MM-dd HH:mm:ss.fff")
        => DateTime.Now.ToString(format, CultureInfo.InvariantCulture);

    private static string Escape(string s) => s.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Flush();
            _writer.Dispose();
        }
    }
}
