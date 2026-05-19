using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Constants;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Domain.Identity;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

public class InventoriesController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IAccessControlService _accessControlService;
    private readonly ICustomIdGenerator _customIdGenerator;
    private readonly UserManager<ApplicationUser> _userManager;

    public InventoriesController(IInventoryService inventoryService, IAccessControlService accessControlService, ICustomIdGenerator customIdGenerator, UserManager<ApplicationUser> userManager)
    {
        _inventoryService = inventoryService;
        _accessControlService = accessControlService;
        _customIdGenerator = customIdGenerator;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var inventories = await _inventoryService.GetAllAsync();
        return View(inventories);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        var inventory = await _inventoryService.GetByIdAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }
        ViewBag.CanManage = await _accessControlService.CanManageAsync(inventory, User);
        return View(inventory);
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new InventoryFormViewModel { AccessMode = InventoryAccessMode.PublicWrite });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Title = model.Title,
            DescriptionMarkdown = model.DescriptionMarkdown,
            CategoryId = model.CategoryId,
            ImageUrl = model.ImageUrl,
            AccessMode = model.AccessMode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        inventory.AccessList.Add(new InventoryAccess
        {
            InventoryId = inventory.Id,
            UserId = userId,
            AccessLevel = AccessLevel.Owner
        });

        await _inventoryService.AddAsync(inventory);
        return RedirectToAction(nameof(Details), new { id = inventory.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        var model = new InventoryFormViewModel
        {
            Id = inventory.Id,
            Title = inventory.Title,
            DescriptionMarkdown = inventory.DescriptionMarkdown,
            CategoryId = inventory.CategoryId,
            ImageUrl = inventory.ImageUrl,
            AccessMode = inventory.AccessMode,
            RowVersion = inventory.RowVersion
        };

        await PopulateCategoriesAsync();
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InventoryFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(model);
        }

        inventory.Title = model.Title;
        inventory.DescriptionMarkdown = model.DescriptionMarkdown;
        inventory.CategoryId = model.CategoryId;
        inventory.ImageUrl = model.ImageUrl;
        inventory.AccessMode = model.AccessMode;

        if (model.RowVersion is null)
        {
            ModelState.AddModelError(string.Empty, "The inventory version is missing. Please retry.");
            await PopulateCategoriesAsync();
            return View(model);
        }

        var updated = await _inventoryService.UpdateAsync(inventory, model.RowVersion.Value);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "The inventory was updated by another user. Please retry.");
            await PopulateCategoriesAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = inventory.Id });
    }

    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        return View(inventory);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        await _inventoryService.DeleteAsync(inventory);
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Access(Guid id)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        var writers = await _accessControlService.GetWritersAsync(id);
        return View(new InventoryAccessViewModel
        {
            Inventory = inventory,
            Writers = writers
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAccess(Guid id, InventoryAccessViewModel model)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        var result = await _accessControlService.AddWriterAsync(id, model.UserIdentifier ?? string.Empty);
        if (!result.Succeeded)
        {
            var writers = await _accessControlService.GetWritersAsync(id);
            return View("Access", new InventoryAccessViewModel
            {
                Inventory = inventory,
                Writers = writers,
                UserIdentifier = model.UserIdentifier,
                ErrorMessage = result.ErrorMessage
            });
        }

        return RedirectToAction(nameof(Access), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAccess(Guid id, string userId)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        await _accessControlService.RemoveWriterAsync(id, userId);
        return RedirectToAction(nameof(Access), new { id });
    }

    [Authorize]
    public async Task<IActionResult> CustomIds(Guid id)
    {
        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        var pattern = await _inventoryService.GetLatestCustomIdPatternAsync(id);
        var preview = await _customIdGenerator.GenerateAsync(inventory);
        ViewBag.PatternOptions = GetPatternOptions();
        return View(new CustomIdPatternViewModel
        {
            InventoryId = id,
            Pattern = pattern,
            Preview = preview
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomIds(Guid id, CustomIdPatternViewModel model)
    {
        if (id != model.InventoryId)
        {
            return NotFound();
        }

        var inventory = await _inventoryService.GetByIdForEditAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!await CanManageAsync(inventory))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.Preview = await _customIdGenerator.GenerateAsync(inventory);
            ViewBag.PatternOptions = GetPatternOptions();
            return View(model);
        }

        var selectedPattern = string.IsNullOrWhiteSpace(model.Pattern)
            ? CustomIdDefaults.DefaultPattern
            : model.Pattern.Trim();
        await _inventoryService.AddCustomIdPatternAsync(id, selectedPattern);
        return RedirectToAction(nameof(CustomIds), new { id });
    }

    private static IReadOnlyList<SelectListItem> GetPatternOptions()
    {
        return new List<SelectListItem>
        {
            new(CustomIdDefaults.DefaultPattern, CustomIdDefaults.DefaultPattern),
            new("FIX:EQ-|SEQ:5", "FIX:EQ-|SEQ:5"),
            new("FIX:LIB-|DATE:yyyy-|SEQ:4", "FIX:LIB-|DATE:yyyy-|SEQ:4"),
            new("FIX:LAP-|DATE:yyyyMMdd-|R6", "FIX:LAP-|DATE:yyyyMMdd-|R6"),
            new("GUID", "GUID")
        };
    }

    private async Task<bool> CanManageAsync(Inventory inventory)
    {
        return await _accessControlService.CanManageAsync(inventory, User);
    }

    private async Task PopulateCategoriesAsync()
    {
        var categories = await _inventoryService.GetCategoriesAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}
