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

        var feed = JsonSerializer.Deserialize<NavFeed>(json)
            ?? throw new InvalidOperationException("NAV returned an empty or invalid feed.");

        Console.WriteLine($"Next URL: {feed.NextUrl}");

        if (!string.IsNullOrEmpty(feed.NextUrl))
        {
            string nextUrl = "https://pam-stilling-feed.nav.no" + feed.NextUrl;

            HttpResponseMessage nextResponse =
                await _httpClient.GetAsync(nextUrl);

            Console.WriteLine($"Next response: {nextResponse.StatusCode}");

            string nextJson = await nextResponse.Content.ReadAsStringAsync();

            var nextFeed = JsonSerializer.Deserialize<NavFeed>(nextJson)
                ?? throw new InvalidOperationException("NAV returned an empty or invalid second feed.");

            int ranaCount = nextFeed.Items.Count(x =>
                x.FeedEntry?.Municipal == "RANA");

            Console.WriteLine($"Next page advertisements: {nextFeed.Items.Count}");
            Console.WriteLine($"Next page RANA advertisements: {ranaCount}");
            Console.WriteLine($"Next page Next URL: {nextFeed.NextUrl}");

            var ranaJob = nextFeed.Items
                .FirstOrDefault(x => x.FeedEntry?.Municipal == "RANA");

            if (ranaJob != null)
            {
                Console.WriteLine($"RANA job ID: {ranaJob.Id}");
                Console.WriteLine($"RANA job URL: {ranaJob.Url}");
                HttpResponseMessage detailResponse = await _httpClient.GetAsync(
                    "https://pam-stilling-feed.nav.no" + ranaJob.Url);

                Console.WriteLine($"Detail response: {detailResponse.StatusCode}");

                string detailJson = await detailResponse.Content.ReadAsStringAsync();

                Console.WriteLine(detailJson);
            }
        }

        return feed;
    }
}