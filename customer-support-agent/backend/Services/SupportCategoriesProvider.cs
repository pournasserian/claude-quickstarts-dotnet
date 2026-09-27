using System.Text.Json;
using CustomerSupportAgent.Api.Models;

namespace CustomerSupportAgent.Api.Services;

public interface ISupportCategoriesProvider
{
    IReadOnlyList<SupportCategory> Categories { get; }
}

public sealed class SupportCategoriesProvider : ISupportCategoriesProvider
{
    public IReadOnlyList<SupportCategory> Categories { get; }

    public SupportCategoriesProvider(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "customer_support_categories.json");
        var json = File.ReadAllText(path);
        var parsed = JsonSerializer.Deserialize<SupportCategoriesFile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Categories = parsed?.Categories ?? Array.Empty<SupportCategory>();
    }
}
