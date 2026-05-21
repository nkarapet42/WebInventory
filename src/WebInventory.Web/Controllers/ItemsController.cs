using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

[Authorize]
public class ItemsController : Controller
{
    private readonly IItemService _itemService;
    private readonly IInventoryService _inventoryService;
    private readonly IAccessControlService _accessControlService;
    private readonly ICustomIdGenerator _customIdGenerator;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ItemsController(
        IItemService itemService,
        IInventoryService inventoryService,
        IAccessControlService accessControlService,
        ICustomIdGenerator customIdGenerator,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _itemService = itemService;
        _inventoryService = inventoryService;
        _accessControlService = accessControlService;
        _customIdGenerator = customIdGenerator;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(Guid inventoryId)
    {
        var inventory = await _inventoryService.GetByIdAsync(inventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = await CanWriteAsync(inventory);
        ViewBag.Fields = await GetFieldsAsync(inventoryId);
        var items = await _itemService.GetByInventoryAsync(inventoryId);
        var itemIds = items.Select(item => item.Id).ToArray();
        var creatorIds = items
            .Select(item => item.CreatedByUserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct()
            .ToArray();
        ViewBag.CreatorNames = creatorIds.Length == 0
            ? new Dictionary<string, string>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(user => creatorIds.Contains(user.Id))
                .Select(user => new { user.Id, Name = user.UserName ?? user.Email ?? "User" })
                .ToDictionaryAsync(user => user.Id, user => user.Name);
        ViewBag.LikeCounts = await _dbContext.ItemLikes
            .AsNoTracking()
            .Where(like => itemIds.Contains(like.ItemId))
            .GroupBy(like => like.ItemId)
            .Select(group => new { ItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.ItemId, row => row.Count);

        var userId = _userManager.GetUserId(User);
        var likedItemIds = userId is null
            ? new List<Guid>()
            : await _dbContext.ItemLikes
                .AsNoTracking()
                .Where(like => like.UserId == userId && itemIds.Contains(like.ItemId))
                .Select(like => like.ItemId)
                .ToListAsync();
        ViewBag.LikedItemIds = likedItemIds.ToHashSet();

        return View(items);
    }

    public async Task<IActionResult> Create(Guid inventoryId)
    {
        var inventory = await _inventoryService.GetByIdAsync(inventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = true;
        ViewBag.Fields = await GetFieldsAsync(inventoryId);
        return View(new ItemFormViewModel { InventoryId = inventoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel model)
    {
        var inventory = await _inventoryService.GetByIdAsync(model.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            ViewBag.Fields = await GetFieldsAsync(model.InventoryId);
            return View(model);
        }

        var customId = string.IsNullOrWhiteSpace(model.CustomId)
            ? await _customIdGenerator.GenerateAsync(inventory)
            : model.CustomId!.Trim();
        var userId = _userManager.GetUserId(User);

        var item = new Item
        {
            Id = Guid.NewGuid(),
            InventoryId = model.InventoryId,
            CreatedByUserId = userId,
            CustomId = customId,
            Text1 = model.Text1,
            Text2 = model.Text2,
            Text3 = model.Text3,
            Multiline1 = model.Multiline1,
            Multiline2 = model.Multiline2,
            Multiline3 = model.Multiline3,
            Num1 = model.Num1,
            Num2 = model.Num2,
            Num3 = model.Num3,
            Doc1 = model.Doc1,
            Doc2 = model.Doc2,
            Doc3 = model.Doc3,
            Bool1 = model.Bool1,
            Bool2 = model.Bool2,
            Bool3 = model.Bool3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _itemService.AddAsync(item);
        return RedirectToAction(nameof(Index), new { inventoryId = model.InventoryId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _itemService.GetByIdForEditAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdAsync(item.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = true;
        ViewBag.Fields = await GetFieldsAsync(item.InventoryId);
        return View(new ItemFormViewModel
        {
            Id = item.Id,
            InventoryId = item.InventoryId,
            CustomId = item.CustomId,
            Text1 = item.Text1,
            Text2 = item.Text2,
            Text3 = item.Text3,
            Multiline1 = item.Multiline1,
            Multiline2 = item.Multiline2,
            Multiline3 = item.Multiline3,
            Num1 = item.Num1,
            Num2 = item.Num2,
            Num3 = item.Num3,
            Doc1 = item.Doc1,
            Doc2 = item.Doc2,
            Doc3 = item.Doc3,
            Bool1 = item.Bool1,
            Bool2 = item.Bool2,
            Bool3 = item.Bool3,
            RowVersion = item.RowVersion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ItemFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        var item = await _itemService.GetByIdForEditAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdAsync(item.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            ViewBag.Fields = await GetFieldsAsync(model.InventoryId);
            return View(model);
        }

        if (model.RowVersion is null)
        {
            ModelState.AddModelError(string.Empty, "The item version is missing. Please retry.");
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            ViewBag.Fields = await GetFieldsAsync(model.InventoryId);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.CustomId))
        {
            ModelState.AddModelError(nameof(model.CustomId), "Custom ID is required.");
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            ViewBag.Fields = await GetFieldsAsync(model.InventoryId);
            return View(model);
        }

        item.CustomId = model.CustomId.Trim();
        item.Text1 = model.Text1;
        item.Text2 = model.Text2;
        item.Text3 = model.Text3;
        item.Multiline1 = model.Multiline1;
        item.Multiline2 = model.Multiline2;
        item.Multiline3 = model.Multiline3;
        item.Num1 = model.Num1;
        item.Num2 = model.Num2;
        item.Num3 = model.Num3;
        item.Doc1 = model.Doc1;
        item.Doc2 = model.Doc2;
        item.Doc3 = model.Doc3;
        item.Bool1 = model.Bool1;
        item.Bool2 = model.Bool2;
        item.Bool3 = model.Bool3;

        var updated = await _itemService.UpdateAsync(item, model.RowVersion.Value);
        if (!updated)
        {
            TempData["ItemEditError"] = "The item was updated by another user. Reloaded the latest version.";
            return RedirectToAction(nameof(Edit), new { id = item.Id });
        }

        return RedirectToAction(nameof(Index), new { inventoryId = item.InventoryId });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _itemService.GetByIdForEditAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdAsync(item.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = true;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _itemService.GetByIdForEditAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdAsync(item.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanWriteAsync(inventory))
        {
            return Forbid();
        }

        await _itemService.DeleteAsync(item);
        return RedirectToAction(nameof(Index), new { inventoryId = item.InventoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        var item = await _dbContext.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var existingLike = await _dbContext.ItemLikes
            .FirstOrDefaultAsync(like => like.ItemId == id && like.UserId == userId);

        var liked = existingLike is null;
        if (existingLike is null)
        {
            _dbContext.ItemLikes.Add(new ItemLike
            {
                ItemId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            _dbContext.ItemLikes.Remove(existingLike);
        }

        await _dbContext.SaveChangesAsync();

        var likeCount = await _dbContext.ItemLikes
            .AsNoTracking()
            .CountAsync(like => like.ItemId == id);

        return Json(new
        {
            itemId = id,
            liked,
            likeCount
        });
    }

    private async Task<bool> CanWriteAsync(Inventory inventory)
    {
        return await _accessControlService.CanWriteAsync(inventory, User);
    }

    private async Task<IReadOnlyList<InventoryField>> GetFieldsAsync(Guid inventoryId)
    {
        return await _dbContext.InventoryFields
            .AsNoTracking()
            .Where(field => field.InventoryId == inventoryId)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldType)
            .ThenBy(field => field.SlotNumber)
            .ToListAsync();
    }

    public static string GetItemPropertyName(InventoryField field)
    {
        return field.FieldType switch
        {
            InventoryFieldType.Text => $"Text{field.SlotNumber}",
            InventoryFieldType.Multiline => $"Multiline{field.SlotNumber}",
            InventoryFieldType.Number => $"Num{field.SlotNumber}",
            InventoryFieldType.Document => $"Doc{field.SlotNumber}",
            InventoryFieldType.Boolean => $"Bool{field.SlotNumber}",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.FieldType, "Unsupported field type.")
        };
    }
}
