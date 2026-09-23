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
}