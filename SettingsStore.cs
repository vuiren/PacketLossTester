using System.Text.Json;
using System.Text.Json.Serialization;

namespace PacketLossTester;

/// <summary>
/// Сохраняет/загружает последние введённые пользователем настройки в JSON-файл рядом с exe.
/// Это позволяет при следующем запуске подставлять их как значения по умолчанию,
/// не вводя каждый раз всё заново.
/// </summary>
public static class SettingsStore
{
    // Файл кладём рядом с exe (AppContext.BaseDirectory), а не в текущую рабочую директорию,
    // чтобы настройки не терялись при запуске из другой папки.
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "PacketLossTester.settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Пытается загрузить ранее сохранённые настройки. Если файла нет/он повреждён — вернёт настройки по умолчанию.</summary>
    public static AppOptions LoadOrDefault()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppOptions();
            }

            string json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AppOptions>(json, JsonOptions);
            return loaded ?? new AppOptions();
        }
        catch
        {
            // Повреждённый/несовместимый файл настроек не должен ломать запуск приложения —
            // просто откатываемся на значения по умолчанию.
            return new AppOptions();
        }
    }

    public static void Save(AppOptions options)
    {
        try
        {
            string json = JsonSerializer.Serialize(options, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Предупреждение] Не удалось сохранить настройки в {FilePath}: {ex.Message}");
        }
    }
}
