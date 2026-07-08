namespace PacketLossTester;

public interface IPacketSender : IAsyncDisposable
{
    string ProtocolName { get; }

    /// <summary>Отправляет один пакет. При ошибке бросает исключение — вызывающий код решает, логировать/переподключаться.</summary>
    Task SendAsync(byte[] data, CancellationToken ct);
}
