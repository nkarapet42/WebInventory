using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

public class HomeController : Controller
{
    private const int LatestInventoryLimit = 10;
    private const int PopularInventoryLimit = 5;
    private const int TagCloudLimit = 30;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var latest = await BuildInventoryQuery()
            .OrderByDescending(inventory => inventory.UpdatedAt)
            .Take(LatestInventoryLimit)
            .ToListAsync();

        var popular = await BuildInventoryQuery()
            .OrderByDescending(inventory => inventory.ItemCount)
            .ThenByDescending(inventory => inventory.UpdatedAt)
            .Take(PopularInventoryLimit)
            .ToListAsync();

        var tags = await _dbContext.InventoryTags
            .AsNoTracking()
            .GroupBy(inventoryTag => new { inventoryTag.TagId, inventoryTag.Tag!.Name })
            .Select(group => new
            {
                group.Key.Name,
                InventoryCount = group.Count()
            })
            .OrderByDescending(tag => tag.InventoryCount)
            .ThenBy(tag => tag.Name)
            .Take(TagCloudLimit)
            .ToListAsync();

        var maxTagCount = tags.Count == 0 ? 1 : tags.Max(tag => tag.InventoryCount);
        var model = new HomeIndexViewModel
        {
            LatestInventories = latest,
            PopularInventories = popular,
            TagCloud = tags
                .Select(tag => new TagCloudItemViewModel
                {
                    Name = tag.Name,
                    InventoryCount = tag.InventoryCount,
                    Weight = CalculateTagWeight(tag.InventoryCount, maxTagCount)
                })
                .ToList()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private IQueryable<HomeInventoryViewModel> BuildInventoryQuery()
    {
        return _dbContext.Inventories
            .AsNoTracking()
            .Select(inventory => new HomeInventoryViewModel
            {
                Id = inventory.Id,
                Title = inventory.Title,
                DescriptionMarkdown = inventory.DescriptionMarkdown,
                ImageUrl = inventory.ImageUrl,
                CategoryName = inventory.Category == null ? null : inventory.Category.Name,
                CreatorName = _dbContext.Users
                    .Where(user => user.Id == inventory.OwnerId)
                    .Select(user => user.UserName ?? user.Email)
                    .FirstOrDefault(),
                ItemCount = inventory.Items.Count,
                UpdatedAt = inventory.UpdatedAt
            });
    }

    private static int CalculateTagWeight(int count, int maxCount)
    {
        if (maxCount <= 0)
        {
            return 1;
        }

        return Math.Clamp((int)Math.Ceiling(count * 5.0 / maxCount), 1, 5);
    }
}
