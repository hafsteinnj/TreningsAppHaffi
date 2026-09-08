using System.Text.Json.Serialization;

namespace TreningsAppHaffi.Data;

public class NavFeed
{
    [JsonPropertyName("next_url")]
    public string? NextUrl { get; set; }

    [JsonPropertyName("items")]
    public List<NavFeedItem> Items { get; set; } = new();
}

public class NavFeedItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("date_modified")]
    public DateTime DateModified { get; set; }

    [JsonPropertyName("_feed_entry")]
    public NavFeedEntry? FeedEntry { get; set; }
}

public class NavFeedEntry
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("municipal")]
    public string? Municipal { get; set; }

    [JsonPropertyName("sistEndret")]
    public DateTime SistEndret { get; set; }
}