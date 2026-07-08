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
    public static byte[] Build(long seq, int paddingSize)
    {
        string sentAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture); // ISO-8601 с миллисекундами
        string header = $"{seq}|{sentAt}|{paddingSize}|";

        var sb = new StringBuilder(header);
        if (paddingSize > 0)
        {
            sb.Append('X', paddingSize);
        }
        sb.Append('\n'); // разделитель сообщений, важно для TCP-потока

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
