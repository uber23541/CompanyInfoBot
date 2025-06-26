using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CompanyInfoBot;

public record CompanyInfo(string Name, string Address);

public sealed class InnService(HttpClient http, IOptions<BotSettings> opt, ILogger<InnService> log)
{
    public async Task<CompanyInfo?> GetCompanyAsync(string inn, CancellationToken ct)
    {
        var req = new { query = inn, count = 1 };

        http.BaseAddress ??= new Uri("https://suggestions.dadata.ru/");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {opt.Value.DadataToken}");

        using var resp = await http.PostAsJsonAsync("suggestions/api/4_1/rs/findById/party", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            log.LogWarning("Dadata returned {Code} for INN {Inn}", resp.StatusCode, inn);
            return null;
        }

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync(ct));
        var root = doc.RootElement.GetProperty("suggestions");
        if (root.GetArrayLength() == 0) return null;

        var data = root[0].GetProperty("data");
        var name = data.GetProperty("name").GetProperty("full_with_opf").GetString()!;
        var addr = data.GetProperty("address").GetProperty("value").GetString()!;
        return new CompanyInfo(name, addr);
    }
}
