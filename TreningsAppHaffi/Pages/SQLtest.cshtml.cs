using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Pages;


public class SQLtestModel : PageModel
{
    private readonly MyDatabaseContext _context;
    public List<TestEntry> SqlEntries { get; set; } = new(); // to squelch warning.

    [BindProperty]
    [Required(ErrorMessage = "Oppsummering er påkrevd.")]
    [MaxLength(100, ErrorMessage = "Oppsummering kan ikke være lengre enn 100 tegn.")]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Tekst er påkrevd.")]
    [MaxLength(800, ErrorMessage = "Tekst kan ikke være lengre enn 800 tegn.")]
    public string Text { get; set; } = string.Empty;

    [BindProperty]
    [Range(0, 4, ErrorMessage = "Ugyldig type valgt.")]
    public int JobId { get; set; }

    [BindProperty]
    [Range(0, 180, ErrorMessage = "Ugyldig tidsbruk valgt.")]
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

    public async Task<IActionResult> OnPostInsertAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

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
        await _context.SaveChangesAsync();

        return RedirectToPage();
        // reloads and triggers OnGet(), - oppdaterer SqlEntries-listen på siden.
    }
}
