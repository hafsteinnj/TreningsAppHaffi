using Microsoft.AspNetCore.Mvc.RazorPages;
using TreningsAppHaffi.Data;
using TreningsAppHaffi.Services;

namespace TreningsAppHaffi.Pages;

public class NavApiListModel : PageModel
{
    private readonly NavApiClient _navApiClient;

    public NavFeed? Feed { get; set; }

    public NavApiListModel(NavApiClient navApiClient)
    {
        _navApiClient = navApiClient;
    }

    public async Task OnGetAsync()
    {
        Feed = await _navApiClient.GetFeedAsync();
    }
}