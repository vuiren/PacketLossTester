using System.Globalization;
using PacketLossTester;

Console.WriteLine("=== PacketLossTester ===");
Console.WriteLine();

// Подгружаем настройки предыдущего запуска (если есть) и используем их как значения по умолчанию.
var previous = SettingsStore.LoadOrDefault();
var options = ConsoleInput.PromptOptions(previous);

// Сохраняем то, что ввёл пользователь, — при следующем запуске это станет умолчаниями.
SettingsStore.Save(options);

Console.WriteLine("=== Параметры теста ===");
Console.WriteLine($"Хост:      {options.Host}");
Console.WriteLine($"Порт:      {options.Port}");
Console.WriteLine($"Протокол:  {options.Protocol}");
Console.WriteLine($"Интервал:  {options.IntervalMs} мс");
Console.WriteLine($"Кол-во:    {(options.Count == 0 ? "бесконечно (Ctrl+C для остановки)" : options.Count.ToString(CultureInfo.InvariantCulture))}");
Console.WriteLine($"Лог-файл:  {Path.GetFullPath(options.LogFile)}");
Console.WriteLine("=========================");
Console.WriteLine();

using var logger = new Logger(options.LogFile);
logger.LogInfo($"Старт теста: host={options.Host} port={options.Port} protocol={options.Protocol} interval={options.IntervalMs}ms count={options.Count}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // не даём процессу убиться мгновенно — завершаемся по своей логике
    Console.WriteLine();
    Console.WriteLine("Получен Ctrl+C, останавливаемся...");
    cts.Cancel();
};

IPacketSender sender = options.Protocol switch
{
    TransportProtocol.Udp => new UdpPacketSender(options.Host, options.Port),
    TransportProtocol.Tcp => new TcpPacketSender(options.Host, options.Port, options.TimeoutMs),
    _ => throw new InvalidOperationException("Неизвестный протокол")
};

string destination = $"{options.Host}:{options.Port}";
long seq = 0;
long sentOk = 0;
long sentFailed = 0;

try
{
    while (!cts.IsCancellationRequested && (options.Count == 0 || seq < options.Count))
    {
        seq++;
        byte[] data = Packet.Build(seq, options.PayloadSize);

        try
        {
            await sender.SendAsync(data, cts.Token);
            logger.LogSent(seq, sender.ProtocolName, destination, data.Length);
            sentOk++;
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(seq, sender.ProtocolName, destination, ex.Message, data.Length);
            sentFailed++;
        }

        try
        {
            await Task.Delay(options.IntervalMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}
finally
{
    await sender.DisposeAsync();
}

Console.WriteLine();
Console.WriteLine("=== Итоги ===");
Console.WriteLine($"Отправлено пакетов (всего попыток): {seq}");
Console.WriteLine($"Успешно отправлено:                 {sentOk}");
Console.WriteLine($"Ошибок при отправке:                {sentFailed}");
Console.WriteLine($"Подробный лог: {Path.GetFullPath(options.LogFile)}");

logger.LogInfo($"Тест завершён. Всего={seq}, успешно={sentOk}, ошибок={sentFailed}");
