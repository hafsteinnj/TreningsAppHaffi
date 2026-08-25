using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Pages;


public class SQLtestModel : PageModel
{
    private readonly MyDatabaseContext _context;
    public List<TestEntry> SqlEntries { get; set; } = new(); // to squelch warning.

    [BindProperty]
    public string Description { get; set; } = string.Empty; // to squelch warning.

    [BindProperty]
    public string Text { get; set; } = string.Empty; // to squelch warning.

    [BindProperty]
    public int JobId { get; set; }

    [BindProperty]
    public int Minutes { get; set; }

    public SQLtestModel(MyDatabaseContext context)
    {
        _context = context;
    }

    public void OnGet()
    {
    }
    public async Task<IActionResult> OnGetCheckConnectionAsync()
    {
        try
        {
            bool canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return new JsonResult(new
                {
                    connected = true,
                    message = "Connected to SQL server."
                });
            }

            return new JsonResult(new
            {
                connected = false,
                message = "SQL server is not currently available."
            });
        }
        catch
        {
            return new JsonResult(new
            {
                connected = false,
                message = "Unable to connect to SQL server."
            });
        }
    }

    public async Task<IActionResult> OnGetEntriesAsync()
    {
        try
        {
            var entries = await _context.TestEntries
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();

            return new JsonResult(entries);
        }
        catch
        {
            return new JsonResult(new
            {
                error = true,
                message = "Unable to retrieve entries from SQL server."
            });
        }
    }

    public IActionResult OnPostInsert()
    {
        var entry = new TestEntry
        {
            CreatedDate = DateTime.UtcNow,

            // Intill jeg har implementert en login-funksjon, setter jeg UserId til 0. Dette kan være 'Gjest' i fremtiden.
            UserId = 0,

            JobId = JobId,
            Description = Description,
            Text = Text,
            Minutes = Minutes,

            // Dette er ikke fult implementert enda. Vil bli knyttet en 'slett' funksjon for bruker(e) i fremtiden.
            Hidden = false
        };

        _context.TestEntries.Add(entry);
        _context.SaveChanges();

        return RedirectToPage(); 
        // reloads and triggers OnGet(), - oppdaterer SqlEntries-listen på siden.
    }
}