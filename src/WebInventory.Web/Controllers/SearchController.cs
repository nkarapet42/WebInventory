using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebInventory.Application.Interfaces;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

[AllowAnonymous]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<IActionResult> Index(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchViewModel { Query = q ?? string.Empty });
        }

        var results = await _searchService.SearchAsync(q);
        return View(new SearchViewModel
        {
            Query = q,
            Inventories = results.Inventories,
            Items = results.Items,
            Tags = results.Tags
        });
    }
}
