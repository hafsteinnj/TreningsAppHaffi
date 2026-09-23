using System.Net.Http.Headers;
using System.Text.Json;
using TreningsAppHaffi.Data;
//using System.Linq;

namespace TreningsAppHaffi.Services;

public class NavApiClient
{
    private readonly HttpClient _httpClient;
    public List<string> LastMunicipalities { get; private set; } = new();
    public int LastRanaCount { get; private set; }
    public int LastActiveRanaCount { get; private set; }
    public int LastRanaMissingUrl { get; private set; }
    public int LastRanaDetailFailed { get; private set; }
    public int LastRanaDetailJsonMissing { get; private set; }
    public string? LastDetailJson { get; private set; }

    public NavApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<NavJob>> GetRanaJobsAsync()
    {
        LastMunicipalities.Clear();
        LastRanaCount = 0;
        LastActiveRanaCount = 0;
        LastRanaMissingUrl = 0;
        LastRanaDetailFailed = 0;
        LastRanaDetailJsonMissing = 0;
        LastDetailJson = null;

        // Get the current public API token.
        string token = await _httpClient.GetStringAsync(
            "https://pam-stilling-feed.nav.no/api/publicToken");

        const string prefix =
            "Current public token for Nav Job Vacancy Feed:";

        token = token
            .Replace(prefix, "")
            .Trim();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Start three months back.
        /*
        DateTimeOffset startDate =
            DateTimeOffset.UtcNow.AddMonths(-3);

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            startDate;
        */
        DateTimeOffset oneDayAgo =
        DateTimeOffset.UtcNow.AddDays(-1);

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            oneDayAgo;

        var ranaJobs = new List<NavJob>();
        int ranaCount = 0; //testing
        int activeRanaCount = 0; //testing

        string? currentUrl = "/api/v1/feed";

        while (!string.IsNullOrEmpty(currentUrl))
        {
            string pageUrl =
                "https://pam-stilling-feed.nav.no" + currentUrl;

            HttpResponseMessage response =
                await _httpClient.GetAsync(pageUrl);

            response.EnsureSuccessStatusCode();

            string json =
                await response.Content.ReadAsStringAsync();

            var feed =
                JsonSerializer.Deserialize<NavFeed>(json)
                ?? throw new InvalidOperationException(
                    "NAV returned an empty or invalid feed.");

            foreach (NavFeedItem item in feed.Items)
            {
                string? municipality = item.FeedEntry?.Municipal;

                if (!string.IsNullOrEmpty(municipality) &&
                    !LastMunicipalities.Contains(municipality))
                {
                    LastMunicipalities.Add(municipality);
                }
                if (municipality == "RANA")
                {
                    ranaCount++;

                    if (item.FeedEntry?.Status == "ACTIVE")
                    {
                        activeRanaCount++;
                    }
                }

                // We only care about active RANA advertisements.
                if (item.FeedEntry?.Status != "ACTIVE")
                    continue;

                if (item.FeedEntry.Municipal != "RANA") // Kan evt endre dette hvis jeg flytter.
                    continue;

                if (string.IsNullOrEmpty(item.Url))
                {
                    LastRanaMissingUrl++;
                    continue;
                }

                // Get the full advertisement.
                HttpResponseMessage detailResponse =
                    await _httpClient.GetAsync(
                        "https://pam-stilling-feed.nav.no" + item.Url);

                if (!detailResponse.IsSuccessStatusCode)
                {
                    LastRanaDetailFailed++;
                    continue;
                }

                string detailJson =
                    await detailResponse.Content.ReadAsStringAsync();

                if (LastDetailJson == null)
                {
                    LastDetailJson = detailJson;
                }

                var detail =
                    JsonSerializer.Deserialize<NavJobDetail>(detailJson);

                if (detail == null)
                {
                    throw new InvalidOperationException(
                        "NAV detail could not be deserialized.");
                }

                if (detail.AdContent == null)
                {
                    LastRanaDetailJsonMissing++;
                    continue;
                }

                ranaJobs.Add(new NavJob
                {
                    NavId = item.FeedEntry.Uuid ?? item.Id ?? "",
                    Title = detail.AdContent.Title
                            ?? item.Title
                            ?? "",

                    Employer = detail.AdContent.Employer?.Name
                               ?? item.FeedEntry.BusinessName,

                    Municipality = item.FeedEntry.Municipal,

                    PublishedDate = detail.AdContent.Published,

                    Deadline = DateTime.TryParse(
                        detail.AdContent.ApplicationDue,
                        out DateTime deadline)
                            ? deadline
                            : null,

                    Position = detail.AdContent.JobTitle,

                    Url = detail.AdContent.Link
                          ?? item.Url,

                    Status = item.FeedEntry.Status,

                    LastModified = item.DateModified,

                    IsNew = true
                });
            }

            currentUrl = feed.NextUrl;
        }
        LastRanaCount = ranaCount;
        LastActiveRanaCount = activeRanaCount;

        return ranaJobs;
    }
}