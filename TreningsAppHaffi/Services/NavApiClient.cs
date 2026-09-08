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

        // --------------------------------------------------
        // TEST 1: Ask for advertisements modified in the
        //         last 3 months.
        // --------------------------------------------------

        DateTimeOffset threeMonthsAgo =
            DateTimeOffset.UtcNow.AddMonths(-3);

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            threeMonthsAgo;

        HttpResponseMessage response = await _httpClient.GetAsync(
            "https://pam-stilling-feed.nav.no/api/v1/feed");

        Console.WriteLine("----- RESPONSE HEADERS -----");
        Console.WriteLine($"Last-Modified: {response.Content.Headers.LastModified}");
        Console.WriteLine($"ETag: {response.Headers.ETag}");
        Console.WriteLine("----------------------------");

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        var feed = JsonSerializer.Deserialize<NavFeed>(json)
            ?? throw new InvalidOperationException(
                "NAV returned an empty or invalid feed.");

        Console.WriteLine("----- FIRST REQUEST -----");
        Console.WriteLine($"First page advertisements: {feed.Items.Count}");
        Console.WriteLine($"First page Next URL: {feed.NextUrl}");

        // Find the newest modification time on this page.
        DateTimeOffset newestModification =
            feed.Items.Max(x =>
                new DateTimeOffset(x.DateModified));
        DateTimeOffset oldestModification =
            feed.Items.Min(x =>
                new DateTimeOffset(x.DateModified));

        Console.WriteLine(
            $"Oldest modification on first page: {oldestModification}");

        Console.WriteLine(
            $"Newest modification on first page: {newestModification}");

        // --------------------------------------------------
        // TEST 2: Ask NAV for changes since the newest
        //         modification we just received.
        // --------------------------------------------------

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            response.Content.Headers.LastModified;

        HttpResponseMessage secondResponse = await _httpClient.GetAsync(
            "https://pam-stilling-feed.nav.no/api/v1/feed");

        Console.WriteLine("----- SECOND REQUEST -----");
        Console.WriteLine($"Status: {(int)secondResponse.StatusCode} {secondResponse.StatusCode}");
        Console.WriteLine("--------------------------");

        // --------------------------------------------------
        // TEST 3: Follow several pages through next_url.
        // --------------------------------------------------

        string? currentUrl = feed.NextUrl;

        for (int pageNumber = 2; pageNumber <= 5; pageNumber++)
        {
            if (string.IsNullOrEmpty(currentUrl))
            {
                Console.WriteLine("No more pages.");
                break;
            }

            string pageUrl =
                "https://pam-stilling-feed.nav.no" + currentUrl;

            HttpResponseMessage pageResponse =
                await _httpClient.GetAsync(pageUrl);

            pageResponse.EnsureSuccessStatusCode();

            string pageJson =
                await pageResponse.Content.ReadAsStringAsync();

            var pageFeed =
                JsonSerializer.Deserialize<NavFeed>(pageJson)
                ?? throw new InvalidOperationException(
                    "NAV returned an empty or invalid page.");

            DateTimeOffset pageoldestModification =
                pageFeed.Items.Min(x =>
                    new DateTimeOffset(x.DateModified));

            DateTimeOffset pageNewestModification =
                pageFeed.Items.Max(x =>
                    new DateTimeOffset(x.DateModified));

            Console.WriteLine($"----- PAGE {pageNumber} -----");
            Console.WriteLine(
                $"Advertisements: {pageFeed.Items.Count}");
            Console.WriteLine(
                $"Last-Modified: {pageResponse.Content.Headers.LastModified}");
            Console.WriteLine(
                $"Oldest modification: {pageoldestModification}");
            Console.WriteLine(
                $"Newest modification: {pageNewestModification}");
            Console.WriteLine(
                $"Next URL: {pageFeed.NextUrl}");
            Console.WriteLine("--------------------------");

            currentUrl = pageFeed.NextUrl;
        }

        Console.WriteLine("-------------------------");

        return feed;
    }
}