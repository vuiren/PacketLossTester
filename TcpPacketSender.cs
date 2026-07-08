using System.Net.Sockets;

namespace PacketLossTester;

/// <summary>
/// TCP-отправитель. TCP сам гарантирует доставку и порядок байт на транспортном уровне,
/// поэтому "потери пакетов" здесь проявляются не как молчаливая пропажа данных,
/// а как обрывы соединения, задержки/ретрансмиты или ошибки записи в сокет.
/// Класс автоматически переподключается, если соединение разорвано.
/// </summary>
public sealed class TcpPacketSender(string host, int port, int timeoutMs) : IPacketSender
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string ProtocolName => "TCP";

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_client is { Connected: true })
        {
            return;
        }

        _client?.Dispose();
        _client = new TcpClient
        {
            SendTimeout = timeoutMs,
            ReceiveTimeout = timeoutMs
        };

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(timeoutMs);

        await _client.ConnectAsync(host, port, connectCts.Token);
        _stream = _client.GetStream();
    }

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        try
        {
            await EnsureConnectedAsync(ct);
            await _stream!.WriteAsync(data, ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), ct);
        }
        catch
        {
            // Соединение могло разорваться — сбрасываем, чтобы следующая отправка попыталась переподключиться.
            _stream?.Dispose();
            _client?.Dispose();
            _client = null;
            _stream = null;
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
