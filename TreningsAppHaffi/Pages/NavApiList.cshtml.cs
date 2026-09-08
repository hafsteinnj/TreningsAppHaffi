using Microsoft.AspNetCore.Mvc.RazorPages;
using TreningsAppHaffi.Services;

namespace TreningsAppHaffi.Pages;

public class NavApiListModel : PageModel
{
    private readonly NavApiClient _navApiClient;

    public string? ApiResult { get; set; }

    public NavApiListModel(NavApiClient navApiClient)
    {
        _navApiClient = navApiClient;
    }

    public async Task OnGetAsync()
    {
        ApiResult = await _navApiClient.GetFeedAsync();
    }
}