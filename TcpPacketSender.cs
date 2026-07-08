using System.Net.Sockets;

namespace PacketLossTester;

/// <summary>
/// TCP-отправитель. TCP сам гарантирует доставку и порядок байт на транспортном уровне,
/// поэтому "потери пакетов" здесь проявляются не как молчаливая пропажа данных,
/// а как обрывы соединения, задержки/ретрансмиты или ошибки записи в сокет.
///
/// ВАЖНО: TcpClient.Connected отражает состояние на момент последней операции,
/// а не текущее в реальном времени — если ничего не читать/писать, обрыв
/// соединения удалённой стороной можно не заметить долго. Поэтому перед каждой
/// отправкой делаем активную проверку сокета через Socket.Poll, плюс включаем
/// TCP keep-alive, чтобы ОС сама обнаруживала "зависшие" соединения.
///
/// Класс автоматически переподключается, если соединение разорвано.
/// </summary>
public sealed class TcpPacketSender(string host, int port, int timeoutMs) : IPacketSender
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string ProtocolName => "TCP";

    /// <summary>
    /// Активная проверка "жив ли сокет". Poll(0, SelectRead) возвращает true, если сокет
    /// либо готов к чтению (есть данные), либо разорван/закрыт удалённой стороной.
    /// Если он "readable", но Available == 0 — данных нет, значит соединение закрыто.
    /// </summary>
    private bool IsConnectionAlive()
    {
        if (_client is not { Connected: true } || _stream is null)
        {
            return false;
        }

        try
        {
            bool readableOrClosed = _client.Client.Poll(0, SelectMode.SelectRead);
            bool hasData = _client.Client.Available > 0;
            return !readableOrClosed || hasData;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (IsConnectionAlive())
        {
            return;
        }

        _stream?.Dispose();
        _client?.Dispose();

        _client = new TcpClient
        {
            SendTimeout = timeoutMs,
            ReceiveTimeout = timeoutMs
        };

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(timeoutMs);

        await _client.ConnectAsync(host, port, connectCts.Token);

        // Включаем TCP keep-alive, чтобы ОС активно проверяла соединение и быстрее
        // обнаруживала обрыв, даже если мы долго ничего не читаем из сокета.
        try
        {
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 5);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 2);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }
        catch
        {
            // Не все платформы/версии поддерживают все опции keep-alive — не критично, если не применились.
        }

        _stream = _client.GetStream();
    }

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        try
        {
            await EnsureConnectedAsync(ct);
            await _stream!.WriteAsync(data, ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), ct);

            if (!IsConnectionAlive())
            {
                throw new IOException("Удалённая сторона закрыла TCP-соединение (обнаружено после записи).");
            }
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
