using System.Text.Json;

namespace KursoftApiClient.Configuration;

/// <summary>
/// KURSOFT API'ye bağlanmak için gereken üç değer: BaseUrl, Username, Password.
///
/// Değerler şu öncelik sırasıyla okunur (üsttekiler alttakileri ezer):
///   1. Ortam değişkenleri: KURSOFT_BASEURL, KURSOFT_USERNAME, KURSOFT_PASSWORD
///   2. appsettings.Local.json  (varsa; bu dosya .gitignore'da olduğu için asla commit edilmez)
///   3. appsettings.json        (repo'da boş placeholder olarak durur, commit edilmesi güvenlidir)
///
/// Bu tasarımın amacı: örnek projeyi klonlayan kimsenin yanlışlıkla kendi
/// kullanıcı adı/şifresini git'e commit etmesini pratik olarak imkansız hale getirmek.
/// </summary>
public sealed class ApiSettings
{
    public required string BaseUrl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }

    public static ApiSettings Load(string basePath)
    {
        var values = new Dictionary<string, string?>
        {
            ["BaseUrl"] = null,
            ["Username"] = null,
            ["Password"] = null,
        };

        // 3) appsettings.json (her zaman var olması beklenir)
        MergeFromJsonFile(values, Path.Combine(basePath, "appsettings.json"));

        // 2) appsettings.Local.json (opsiyonel, sadece geliştiricinin kendi makinesinde)
        MergeFromJsonFile(values, Path.Combine(basePath, "appsettings.Local.json"));

        // 1) Ortam değişkenleri (en yüksek öncelik — CI/CD veya sunucu ortamları için idealdir)
        values["BaseUrl"] = Environment.GetEnvironmentVariable("KURSOFT_BASEURL") ?? values["BaseUrl"];
        values["Username"] = Environment.GetEnvironmentVariable("KURSOFT_USERNAME") ?? values["Username"];
        values["Password"] = Environment.GetEnvironmentVariable("KURSOFT_PASSWORD") ?? values["Password"];

        var missing = values.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Eksik ayarlar: {string.Join(", ", missing)}. " +
                "Bunları appsettings.Local.json içinde ya da KURSOFT_BASEURL / KURSOFT_USERNAME / " +
                "KURSOFT_PASSWORD ortam değişkenleri olarak tanımlayın. Detaylar için README.md dosyasına bakın.");
        }

        return new ApiSettings
        {
            BaseUrl = values["BaseUrl"]!,
            Username = values["Username"]!,
            Password = values["Password"]!,
        };
    }

    private static void MergeFromJsonFile(Dictionary<string, string?> values, string path)
    {
        if (!File.Exists(path))
            return;

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);

        foreach (var key in values.Keys.ToList())
        {
            if (doc.RootElement.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.String)
            {
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    values[key] = text;
            }
        }
    }
}
