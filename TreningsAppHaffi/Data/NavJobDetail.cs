using System.Text.Json.Serialization;

namespace TreningsAppHaffi.Data;

public class NavJobDetail
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("json")]
    public NavJobContent? Json { get; set; }

    [JsonPropertyName("sistEndret")]
    public DateTime? SistEndret { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class NavJobContent
{
    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("jobtitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("applicationDue")]
    public DateTime? ApplicationDue { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("employer")]
    public NavEmployer? Employer { get; set; }
}

public class NavEmployer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}