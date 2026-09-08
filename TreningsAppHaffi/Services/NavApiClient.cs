using System.Net.Http.Headers;
using System.Text.Json;
using TreningsAppHaffi.Data;
//using System.Linq;

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
        // Get the current public API token.
        string token = await _httpClient.GetStringAsync(
            "https://pam-stilling-feed.nav.no/api/publicToken");

        const string prefix = "Current public token for Nav Job Vacancy Feed:";

        token = token
            .Replace(prefix, "")
            .Trim();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Only ask NAV for entries modified within the last year.
        DateTimeOffset oneYearAgo =
            DateTimeOffset.UtcNow.AddYears(-1);

        _httpClient.DefaultRequestHeaders.IfModifiedSince = oneYearAgo;

        // Get the first page.
        HttpResponseMessage response = await _httpClient.GetAsync(
            "https://pam-stilling-feed.nav.no/api/v1/feed");

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        var feed = JsonSerializer.Deserialize<NavFeed>(json)
            ?? throw new InvalidOperationException(
                "NAV returned an empty or invalid feed.");

        // Temporary test statistics.
        int totalCount = 0;
        int inactiveCount = 0;
        int activeRanaCount = 0;
        int pageCount = 0;

        // Process the first page.
        pageCount++;

        totalCount += feed.Items.Count;

        inactiveCount += feed.Items.Count(x =>
            x.FeedEntry?.Status == "INACTIVE");

        activeRanaCount += feed.Items.Count(x =>
            x.FeedEntry?.Status == "ACTIVE" &&
            x.FeedEntry?.Municipal == "RANA");

        // Follow the remaining pages.
        string? nextUrl = feed.NextUrl;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            string fullNextUrl =
                "https://pam-stilling-feed.nav.no" + nextUrl;

            HttpResponseMessage nextResponse =
                await _httpClient.GetAsync(fullNextUrl);

            nextResponse.EnsureSuccessStatusCode();

            string nextJson =
                await nextResponse.Content.ReadAsStringAsync();

            var nextFeed =
                JsonSerializer.Deserialize<NavFeed>(nextJson)
                ?? throw new InvalidOperationException(
                    "NAV returned an empty or invalid feed page.");

            pageCount++;

            totalCount += nextFeed.Items.Count;

            inactiveCount += nextFeed.Items.Count(x =>
                x.FeedEntry?.Status == "INACTIVE");

            activeRanaCount += nextFeed.Items.Count(x =>
                x.FeedEntry?.Status == "ACTIVE" &&
                x.FeedEntry?.Municipal == "RANA");

            nextUrl = nextFeed.NextUrl;
        }

        Console.WriteLine("----- NAV FEED TEST -----");
        Console.WriteLine($"Pages received: {pageCount}");
        Console.WriteLine($"Total advertisements: {totalCount}");
        Console.WriteLine($"Inactive advertisements: {inactiveCount}");
        Console.WriteLine($"Active RANA advertisements: {activeRanaCount}");
        Console.WriteLine("-------------------------");

        return feed;
    }
}