using System.Net.Http.Headers;
using System.Text.Json;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Services;

public class NavApiClient
{
    private readonly HttpClient _httpClient;

    public NavApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NavFeed> GetFeedAsync()
    {
        // Get metode for public NAV api token. Kun for testing. Gyldig i 24timer.
        string token = await _httpClient.GetStringAsync(
            "https://pam-stilling-feed.nav.no/api/publicToken");

        const string prefix = "Current public token for Nav Job Vacancy Feed:";

        token = token
            .Replace(prefix, "")
            .Trim();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _httpClient.GetAsync(
            "https://pam-stilling-feed.nav.no/api/v1/feed");

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<NavFeed>(json)
            ?? throw new InvalidOperationException("NAV returned an empty or invalid feed.");
    }
}