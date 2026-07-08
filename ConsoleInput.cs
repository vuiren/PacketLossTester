using System.Globalization;

namespace PacketLossTester;

/// <summary>
/// Хелперы для интерактивного запроса значений у пользователя в консоли.
/// Везде показывается значение по умолчанию (взятое из предыдущего запуска либо встроенное),
/// и если пользователь просто нажимает Enter — используется оно.
/// </summary>
public static class ConsoleInput
{
    public static string PromptString(string label, string defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            return input.Trim();
        }
    }

    public static int PromptInt(string label, int defaultValue, int? min = null, int? max = null)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            if (!int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                Console.WriteLine("Введите целое число.");
                continue;
            }

            if (min.HasValue && value < min.Value)
            {
                Console.WriteLine($"Значение должно быть не меньше {min.Value}.");
                continue;
            }

            if (max.HasValue && value > max.Value)
            {
                Console.WriteLine($"Значение должно быть не больше {max.Value}.");
                continue;
            }

            return value;
        }
    }

    public static long PromptLong(string label, long defaultValue, long? min = null)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            if (!long.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            {
                Console.WriteLine("Введите целое число.");
                continue;
            }

            if (min.HasValue && value < min.Value)
            {
                Console.WriteLine($"Значение должно быть не меньше {min.Value}.");
                continue;
            }

            return value;
        }
    }

    public static TransportProtocol PromptProtocol(string label, TransportProtocol defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} (udp/tcp) [{defaultValue.ToString().ToLowerInvariant()}]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            switch (input.Trim().ToLowerInvariant())
            {
                case "udp":
                    return TransportProtocol.Udp;
                case "tcp":
                    return TransportProtocol.Tcp;
                default:
                    Console.WriteLine("Введите 'udp' или 'tcp'.");
                    continue;
            }
        }
    }

    /// <summary>Опрашивает пользователя по всем полям AppOptions, используя переданные настройки как значения по умолчанию.</summary>
    public static AppOptions PromptOptions(AppOptions defaults)
    {
        Console.WriteLine("=== Настройка теста (Enter — оставить значение по умолчанию) ===");

        var options = new AppOptions
        {
            Host = PromptString("Адрес сервера (host)", defaults.Host),
            Port = PromptInt("Порт сервера", defaults.Port, min: 1, max: 65535),
            Protocol = PromptProtocol("Протокол", defaults.Protocol),
            IntervalMs = PromptInt("Интервал между пакетами, мс", defaults.IntervalMs, min: 1),
            Count = PromptLong("Количество пакетов (0 = бесконечно, до Ctrl+C)", defaults.Count, min: 0),
            PayloadSize = PromptInt("Доп. размер полезной нагрузки, байт", defaults.PayloadSize, min: 0),
            LogFile = PromptString("Путь к файлу лога (CSV)", defaults.LogFile),
            TimeoutMs = PromptInt("Таймаут TCP-соединения/записи, мс", defaults.TimeoutMs, min: 1)
        };

        Console.WriteLine();
        return options;
    }
}
