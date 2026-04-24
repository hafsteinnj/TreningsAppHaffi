using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Pages;


public class SQLtestModel : PageModel
{
    private readonly MyDatabaseContext _context;
    public List<TestEntry> SqlEntries { get; set; }

    [BindProperty]
    public string Description { get; set; }

    [BindProperty]
    public string Text { get; set; }

    public SQLtestModel(MyDatabaseContext context)
    {
        _context = context;
    }

    public void OnGet()
    {
        SqlEntries = _context.TestEntries
                      .OrderByDescending(e => e.CreatedDate)
                      .ToList();
    }

    public IActionResult OnPostInsert()
    {
        var entry = new TestEntry
        {
            CreatedDate = DateTime.Now,
            Description = Description,
            Text = Text
        };

        _context.TestEntries.Add(entry);
        _context.SaveChanges();

        return RedirectToPage(); 
        // reloads and triggers OnGet(), - dvs oppdaterer SqlEntries-listen med den nye raden som nettopp ble lagt til.
    }

    public IActionResult OnPostClear()
    {
        var allEntries = _context.TestEntries.ToList();
        _context.TestEntries.RemoveRange(allEntries);
        _context.SaveChanges();

        return RedirectToPage();
    }
}


