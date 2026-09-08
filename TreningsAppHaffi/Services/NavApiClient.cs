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

        Console.WriteLine(
            $"Newest modification on first page: {newestModification}");

        // --------------------------------------------------
        // TEST 2: Ask NAV for changes since the newest
        //         modification we just received.
        // --------------------------------------------------

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            newestModification;

        HttpResponseMessage secondResponse =
            await _httpClient.GetAsync(
                "https://pam-stilling-feed.nav.no/api/v1/feed");

        Console.WriteLine("----- SECOND REQUEST -----");
        Console.WriteLine(
            $"Second response: {secondResponse.StatusCode}");

        if (secondResponse.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            Console.WriteLine(
                "NAV says there have been no changes.");
        }
        else
        {
            secondResponse.EnsureSuccessStatusCode();

            string secondJson =
                await secondResponse.Content.ReadAsStringAsync();

            var secondFeed =
                JsonSerializer.Deserialize<NavFeed>(secondJson)
                ?? throw new InvalidOperationException(
                    "NAV returned an empty or invalid second feed.");

            Console.WriteLine(
                $"Second page advertisements: {secondFeed.Items.Count}");

            Console.WriteLine(
                $"Second page Next URL: {secondFeed.NextUrl}");
        }

        Console.WriteLine("-------------------------");

        return feed;
    }
}