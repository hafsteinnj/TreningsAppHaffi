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

    public async Task<List<NavJob>> GetRanaJobsAsync()
    {
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
        DateTimeOffset startDate =
            DateTimeOffset.UtcNow.AddMonths(-3);

        _httpClient.DefaultRequestHeaders.IfModifiedSince =
            startDate;

        var ranaJobs = new List<NavJob>();

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
                // We only care about active RANA advertisements.
                if (item.FeedEntry?.Status != "ACTIVE")
                    continue;

                if (item.FeedEntry.Municipal != "RANA") // Kan evt endre dette hvis jeg flytter.
                    continue;

                if (string.IsNullOrEmpty(item.Url))
                    continue;

                // Get the full advertisement.
                HttpResponseMessage detailResponse =
                    await _httpClient.GetAsync(
                        "https://pam-stilling-feed.nav.no" + item.Url);

                if (!detailResponse.IsSuccessStatusCode)
                    continue;

                string detailJson =
                    await detailResponse.Content.ReadAsStringAsync();

                var detail =
                    JsonSerializer.Deserialize<NavJobDetail>(detailJson);

                if (detail == null)
                    continue;

                if (detail.Json == null)
                    continue;

                ranaJobs.Add(new NavJob
                {
                    NavId = item.FeedEntry.Uuid ?? item.Id ?? "",
                    Title = detail.Json.Title
                            ?? item.Title
                            ?? "",

                    Employer = detail.Json.Employer?.Name
                               ?? item.FeedEntry.BusinessName,

                    Municipality = item.FeedEntry.Municipal,

                    PublishedDate = detail.Json.Published,

                    Deadline = detail.Json.ApplicationDue,

                    Position = detail.Json.JobTitle,

                    Url = detail.Json.Link
                          ?? item.Url,

                    Status = item.FeedEntry.Status,

                    LastModified = item.DateModified,

                    IsNew = true
                });
            }

            currentUrl = feed.NextUrl;
        }

        return ranaJobs;
    }
}