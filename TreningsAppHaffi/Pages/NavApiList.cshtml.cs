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
        try
        {
            List<NavJob> jobs =
                await _navApiClient.GetRanaJobsAsync();

            ViewData["SyncResult"] =
                $"NAV returned {jobs.Count} RANA jobs.";

            foreach (NavJob job in jobs
                .GroupBy(job => job.NavId)
                .Select(group => group
                    .OrderByDescending(job => job.LastModified)
                    .First()))
            {
                NavJob? existingJob = await _db.NavJobs
                    .FirstOrDefaultAsync(x => x.NavId == job.NavId);

                if (existingJob == null)
                {
                    _db.NavJobs.Add(job);
                }
                else
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

                    existingJob.IsNew = false;
                }
            }

            ViewData["SyncResult"] +=
                " Preparing to save to SQL.";

            await _db.SaveChangesAsync();

            ViewData["SyncResult"] +=
                " SQL save completed.";

            Jobs = await _db.NavJobs
                .OrderByDescending(job => job.PublishedDate)
                .ToListAsync();

            return Page();
        }
        catch (Exception ex)
        {
            ViewData["SyncError"] = ex.ToString();

            Jobs = await _db.NavJobs
                .OrderByDescending(job => job.PublishedDate)
                .ToListAsync();

            return Page();
        }
    }
}