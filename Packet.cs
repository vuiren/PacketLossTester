using System.Globalization;
using System.Text;

namespace PacketLossTester;

/// <summary>
/// Формирует байтовое содержимое пакета: уникальный возрастающий счётчик + время отправки + опциональный "балласт".
/// Формат текстовый, чтобы принимающую сторону было легко разобрать/пологгировать:
///   SEQ|SENT_AT_ISO8601|PAYLOAD_SIZE|PAYLOAD...
/// </summary>
public static class Packet
{
    public static byte[] Build(long seq)
    {
        string sentAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture); // ISO-8601 с миллисекундами
        return Encoding.UTF8.GetBytes($"{seq}|{sentAt}|\n");
    }
}
