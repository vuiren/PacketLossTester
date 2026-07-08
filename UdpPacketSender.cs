using System.Net.Sockets;

namespace PacketLossTester;

/// <summary>
/// UDP-отправитель. UDP не гарантирует доставку и порядок — это как раз то,
/// что позволяет измерять реальные потери пакетов в сети/у провайдера.
/// </summary>
public sealed class UdpPacketSender : IPacketSender
{
    private readonly UdpClient _client;
    private readonly string _host;
    private readonly int _port;

    public string ProtocolName => "UDP";

    public UdpPacketSender(string host, int port)
    {
        _host = host;
        _port = port;
        _client = new UdpClient();
        // На всякий случай не позволяем ОС долго блокировать закрытие сокета.
        _client.Client.SendTimeout = 3000;
    }

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        await _client.SendAsync(data, data.Length, _host, _port).WaitAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
