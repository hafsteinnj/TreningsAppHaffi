using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TreningsAppHaffi.Data;
using TreningsAppHaffi.Services;

namespace TreningsAppHaffi.Pages;

public class NavApiListModel : PageModel
{
    private readonly MyDatabaseContext _db;
    private readonly NavApiClient _navApiClient;

    public List<NavJob> Jobs { get; set; } = new();

    public NavApiListModel(
        MyDatabaseContext db,
        NavApiClient navApiClient)
    {
        _db = db;
        _navApiClient = navApiClient;
    }

    public async Task OnGetAsync()
    {
        Jobs = await _db.NavJobs
            .OrderByDescending(job => job.PublishedDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        DateTime oneMonthAgo =
            DateTime.UtcNow.AddMonths(-1);

        DateTime latestLastModified =
            await _db.NavJobs
                .Where(job => job.LastModified.HasValue)
                .MaxAsync(job => (DateTime?)job.LastModified)
                ?? oneMonthAgo;

        DateTime syncStart =
            latestLastModified > oneMonthAgo
                ? latestLastModified
                : oneMonthAgo;

        List<NavJob> jobs =
            await _navApiClient.GetRanaJobsAsync(syncStart);

        int addedCount = 0;
        int updatedCount = 0;

        List<NavJob> latestJobs = jobs
            .Where(job => !string.IsNullOrEmpty(job.NavId))
            .GroupBy(job => job.NavId)
            .Select(group => group
                .OrderByDescending(job => job.LastModified)
                .First())
            .ToList();

        foreach (NavJob job in latestJobs)
        {
            NavJob? existingJob = await _db.NavJobs
                .FirstOrDefaultAsync(x => x.NavId == job.NavId);

            if (existingJob == null)
            {
                _db.NavJobs.Add(job);
                addedCount++;
                continue;
            }

            if (job.LastModified.HasValue &&
                (!existingJob.LastModified.HasValue ||
                 job.LastModified.Value > existingJob.LastModified.Value))
            {
                existingJob.Title = job.Title;
                existingJob.Employer = job.Employer;
                existingJob.Municipality = job.Municipality;
                existingJob.PublishedDate = job.PublishedDate;
                existingJob.Deadline = job.Deadline;
                existingJob.Position = job.Position;
                existingJob.Url = job.Url;
                existingJob.Status = job.Status;
                existingJob.LastModified = job.LastModified;
                existingJob.IsNew = true;

                updatedCount++;
            }
        }

        await _db.SaveChangesAsync();

        string syncStartText = syncStart.ToString("dd.MM.yyyy HH:mm");

        ViewData["SyncResult"] =
            $"Sync complete: {addedCount} new, {updatedCount} updated. " +
            $"Pulled jobs since {syncStartText}.";

        Jobs = await _db.NavJobs
            .OrderByDescending(job => job.PublishedDate)
            .ToListAsync();

        return Page();
    }
}