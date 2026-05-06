using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Identity;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

[Authorize]
public class ItemsController : Controller
{
    private readonly IItemService _itemService;
    private readonly IInventoryService _inventoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ItemsController(IItemService itemService, IInventoryService inventoryService, UserManager<ApplicationUser> userManager)
    {
        _itemService = itemService;
        _inventoryService = inventoryService;
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
        ViewBag.CanWrite = CanWrite(inventory);
        var items = await _itemService.GetByInventoryAsync(inventoryId);
        return View(items);
    }

    public async Task<IActionResult> Create(Guid inventoryId)
    {
        var inventory = await _inventoryService.GetByIdAsync(inventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!CanWrite(inventory))
        {
            return Forbid();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = true;
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

        if (!CanWrite(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            return View(model);
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            InventoryId = model.InventoryId,
            CustomId = model.CustomId,
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

        if (!CanWrite(inventory))
        {
            return Forbid();
        }

        ViewBag.Inventory = inventory;
        ViewBag.CanWrite = true;
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

        if (!CanWrite(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            return View(model);
        }

        if (model.RowVersion is null)
        {
            ModelState.AddModelError(string.Empty, "The item version is missing. Please retry.");
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            return View(model);
        }

        item.CustomId = model.CustomId;
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
            ModelState.AddModelError(string.Empty, "The item was updated by another user. Please retry.");
            ViewBag.Inventory = inventory;
            ViewBag.CanWrite = true;
            return View(model);
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

        if (!CanWrite(inventory))
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

        if (!CanWrite(inventory))
        {
            return Forbid();
        }

        await _itemService.DeleteAsync(item);
        return RedirectToAction(nameof(Index), new { inventoryId = item.InventoryId });
    }

    private bool CanWrite(Inventory inventory)
    {
        if (inventory.AccessMode == Domain.Enums.InventoryAccessMode.PublicWrite)
        {
            return User.Identity?.IsAuthenticated == true;
        }

        var userId = _userManager.GetUserId(User);
        return userId is not null && inventory.OwnerId == userId;
    }
}
