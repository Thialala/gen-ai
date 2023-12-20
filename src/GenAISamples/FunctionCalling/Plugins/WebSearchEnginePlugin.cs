using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FunctionCalling.Plugins;

internal class WebSearchEnginePlugin
{
    private readonly string _subscriptionKey;
    private readonly string _customConfigId;

    public WebSearchEnginePlugin(string subscriptionKey, string customConfigId)
    {
        _subscriptionKey = subscriptionKey;
        _customConfigId = customConfigId;
    }


    [KernelFunction, Description("Perform a web search.")]
    public async Task<IEnumerable<string>> SearchAsync(
        [Description("Search query")] string query,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.bing.microsoft.com/v7.0/custom/search?q={query}&customconfig={_customConfigId}";

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

        var httpResponseMessage = await client.GetAsync(url, cancellationToken);
        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
        BingSearchResponse? response = JsonConvert.DeserializeObject<BingSearchResponse>(responseContent);

        WebPage[]? results = response?.WebPages?.Value;

        return results == null ? Enumerable.Empty<string>() : results.Select(x => x.Snippet);
    }
}

// <responseClasses>
[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
        Justification = "Class is instantiated through deserialization.")]
public sealed class BingSearchResponse
{
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
}

[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
    Justification = "Class is instantiated through deserialization.")]
public sealed class WebPages
{
    [JsonPropertyName("value")]
    public WebPage[]? Value { get; set; }
}

[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
    Justification = "Class is instantiated through deserialization.")]
public sealed class WebPage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;
}

// </responseClasses>
