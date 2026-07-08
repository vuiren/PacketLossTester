namespace PacketLossTester;

public enum TransportProtocol
{
    Udp,
    Tcp
}

/// <summary>
/// Настройки приложения. Заполняются интерактивно при запуске (см. ConsoleInput)
/// и сохраняются/подгружаются через SettingsStore, чтобы при следующем запуске
/// подставляться как значения по умолчанию.
/// </summary>
public sealed class AppOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public TransportProtocol Protocol { get; set; } = TransportProtocol.Udp;

    /// <summary>Интервал между отправками пакетов, мс.</summary>
    public int IntervalMs { get; set; } = 1000;

    /// <summary>Количество пакетов для отправки. 0 = отправлять, пока не нажмут Ctrl+C.</summary>
    public long Count { get; set; } = 0;

    /// <summary>Путь к файлу лога.</summary>
    public string LogFile { get; set; } = "packet_log.csv";

    /// <summary>Таймаут TCP-соединения / операций записи, мс.</summary>
    public int TimeoutMs { get; set; } = 3000;
}
