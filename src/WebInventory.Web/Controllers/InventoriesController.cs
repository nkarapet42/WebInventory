using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Constants;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Models;
using WebInventory.Web.Services;

namespace WebInventory.Web.Controllers;

public class InventoriesController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IAccessControlService _accessControlService;
    private readonly ICustomIdGenerator _customIdGenerator;
    private readonly ApplicationDbContext _dbContext;
    private readonly MarkdownService _markdownService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageUploadService _imageUploadService;

    public InventoriesController(
        IInventoryService inventoryService,
        IAccessControlService accessControlService,
        ICustomIdGenerator customIdGenerator,
        ApplicationDbContext dbContext,
        MarkdownService markdownService,
        UserManager<ApplicationUser> userManager,
        IImageUploadService imageUploadService)
    {
        _inventoryService = inventoryService;
        _accessControlService = accessControlService;
        _customIdGenerator = customIdGenerator;
        _dbContext = dbContext;
        _markdownService = markdownService;
        _userManager = userManager;
        _imageUploadService = imageUploadService;
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
        var inventory = await _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inventory is null)
        {
            return NotFound();
        }

        var fields = await _dbContext.InventoryFields
            .AsNoTracking()
            .Where(field => field.InventoryId == id)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldType)
            .ThenBy(field => field.SlotNumber)
            .ToListAsync();

        var itemCount = await _dbContext.Items
            .AsNoTracking()
            .CountAsync(item => item.InventoryId == id);

        var commentCount = await _dbContext.Comments
            .AsNoTracking()
            .CountAsync(comment => comment.InventoryId == id);

        var commentRows = await _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.InventoryId == id)
            .Join(_dbContext.Users,
                comment => comment.UserId,
                user => user.Id,
                (comment, user) => new { Comment = comment, UserName = user.UserName ?? user.Email ?? "User" })
            .OrderBy(row => row.Comment.CreatedAt)
            .ToListAsync();

        var comments = commentRows
            .Select(row => new InventoryCommentViewModel
            {
                Id = row.Comment.Id,
                UserId = row.Comment.UserId,
                UserName = row.UserName,
                BodyHtml = _markdownService.ToHtml(row.Comment.BodyMarkdown),
                CreatedAt = row.Comment.CreatedAt
            })
            .ToList();

        var model = new InventoryDetailsViewModel
        {
            Inventory = inventory,
            CanManage = await _accessControlService.CanManageAsync(inventory, User),
            CanWrite = await _accessControlService.CanWriteAsync(inventory, User),
            ItemCount = itemCount,
            CommentCount = commentCount,
            LatestCustomIdPattern = await _inventoryService.GetLatestCustomIdPatternAsync(id),
            Fields = fields,
            Comments = comments,
            NumericStats = await BuildNumericStatsAsync(id, fields)
        };

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new InventoryFormViewModel { AccessMode = InventoryAccessMode.PublicWrite });
    }

    [AllowAnonymous]
    public async Task<IActionResult> TagSuggestions(string? q)
    {
        var query = (q ?? string.Empty).Trim().ToUpperInvariant();
        if (query.Length == 0)
        {
            return Json(Array.Empty<string>());
        }

        var tags = await _dbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.NormalizedName.StartsWith(query))
            .OrderBy(tag => tag.Name)
            .Select(tag => tag.Name)
            .Take(10)
            .ToListAsync();

        return Json(tags);
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

        model.ImageUrl = await ResolveImageUrlAsync(model);
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
        await UpdateInventoryTagsAsync(inventory.Id, ParseTags(model.Tags));
        await _dbContext.SaveChangesAsync();
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
            Tags = await GetTagStringAsync(inventory.Id),
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

        model.ImageUrl = await ResolveImageUrlAsync(model);
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
        await UpdateInventoryTagsAsync(inventory.Id, ParseTags(model.Tags));

        if (model.RowVersion is null)
        {
            ModelState.AddModelError(string.Empty, "The inventory version is missing. Please retry.");
            await PopulateCategoriesAsync();
            return View(model);
        }

        var updated = await _inventoryService.UpdateAsync(inventory, model.RowVersion.Value);
        if (!updated)
        {
            TempData["InventoryEditError"] = "The inventory was updated by another user. Reloaded the latest version.";
            return RedirectToAction(nameof(Edit), new { id = inventory.Id });
        }

        return RedirectToAction(nameof(Details), new { id = inventory.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Autosave(Guid id, InventoryFormViewModel model)
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
            var errors = ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToArray();
            return BadRequest(new { saved = false, errors });
        }

        if (model.RowVersion is null)
        {
            return Conflict(new { saved = false, message = "The inventory version is missing. Refresh the page and try again." });
        }

        inventory.Title = model.Title;
        inventory.DescriptionMarkdown = model.DescriptionMarkdown;
        inventory.CategoryId = model.CategoryId;
        inventory.ImageUrl = model.ImageUrl;
        inventory.AccessMode = model.AccessMode;
        await UpdateInventoryTagsAsync(inventory.Id, ParseTags(model.Tags));

        var updated = await _inventoryService.UpdateAsync(inventory, model.RowVersion.Value);
        if (!updated)
        {
            return Conflict(new { saved = false, message = "The inventory was updated by another user. Refresh before continuing." });
        }

        var rowVersion = await _dbContext.Inventories
            .AsNoTracking()
            .Where(existing => existing.Id == id)
            .Select(existing => existing.RowVersion)
            .FirstAsync();

        return Json(new
        {
            saved = true,
            rowVersion,
            savedAt = DateTime.UtcNow.ToString("u")
        });
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
    public async Task<IActionResult> Access(Guid id, string sort = "name")
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

        var writers = SortUsers(await _accessControlService.GetWritersAsync(id), sort);
        return View(new InventoryAccessViewModel
        {
            Inventory = inventory,
            Writers = writers,
            SortMode = NormalizeAccessSort(sort)
        });
    }

    [Authorize]
    public async Task<IActionResult> UserSuggestions(Guid id, string? q)
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

        var query = (q ?? string.Empty).Trim().ToUpperInvariant();
        if (query.Length == 0)
        {
            return Json(Array.Empty<object>());
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id != inventory.OwnerId &&
                !_dbContext.InventoryAccesses.Any(access => access.InventoryId == id && access.UserId == user.Id) &&
                ((user.NormalizedUserName != null && user.NormalizedUserName.StartsWith(query)) ||
                 (user.NormalizedEmail != null && user.NormalizedEmail.StartsWith(query))))
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                value = user.Email ?? user.UserName,
                text = (user.UserName ?? "User") + (user.Email == null ? string.Empty : " <" + user.Email + ">")
            })
            .Take(10)
            .ToListAsync();

        return Json(users);
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
            var writers = SortUsers(await _accessControlService.GetWritersAsync(id), model.SortMode);
            return View("Access", new InventoryAccessViewModel
            {
                Inventory = inventory,
                Writers = writers,
                UserIdentifier = model.UserIdentifier,
                SortMode = NormalizeAccessSort(model.SortMode),
                ErrorMessage = result.ErrorMessage
            });
        }

        return RedirectToAction(nameof(Access), new { id, sort = NormalizeAccessSort(model.SortMode) });
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
            return View(model);
        }

        var selectedPattern = string.IsNullOrWhiteSpace(model.Pattern)
            ? CustomIdDefaults.DefaultPattern
            : model.Pattern.Trim();

        var partCount = selectedPattern.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (partCount > CustomIdDefaults.MaxPatternParts)
        {
            ModelState.AddModelError(nameof(model.Pattern), $"Use no more than {CustomIdDefaults.MaxPatternParts} ID parts.");
            model.Preview = await _customIdGenerator.GenerateAsync(inventory);
            return View(model);
        }

        await _inventoryService.AddCustomIdPatternAsync(id, selectedPattern);
        return RedirectToAction(nameof(CustomIds), new { id });
    }

    [Authorize]
    public async Task<IActionResult> Fields(Guid id)
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

        return View(await BuildFieldsViewModelAsync(inventory));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveField(Guid id, InventoryFieldsViewModel model)
    {
        if (id != model.InventoryId || id != model.Form.InventoryId)
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
            var invalidModel = await BuildFieldsViewModelAsync(inventory, model.Form);
            invalidModel.ErrorMessage = "Check the field values and try again.";
            return View("Fields", invalidModel);
        }

        var duplicateSlot = await _dbContext.InventoryFields
            .AsNoTracking()
            .AnyAsync(field =>
                field.InventoryId == id &&
                field.FieldType == model.Form.FieldType &&
                field.SlotNumber == model.Form.SlotNumber &&
                field.Id != model.Form.Id);

        if (duplicateSlot)
        {
            var duplicateModel = await BuildFieldsViewModelAsync(inventory, model.Form);
            duplicateModel.ErrorMessage = "This field type and slot are already used.";
            return View("Fields", duplicateModel);
        }

        var typeCount = await _dbContext.InventoryFields
            .AsNoTracking()
            .CountAsync(field =>
                field.InventoryId == id &&
                field.FieldType == model.Form.FieldType &&
                field.Id != model.Form.Id);

        if (model.Form.Id is null && typeCount >= InventoryLimits.MaxFieldsPerType)
        {
            var limitModel = await BuildFieldsViewModelAsync(inventory, model.Form);
            limitModel.ErrorMessage = $"Only {InventoryLimits.MaxFieldsPerType} fields of each type are supported.";
            return View("Fields", limitModel);
        }

        if (model.Form.Id is null)
        {
            var nextOrder = await _dbContext.InventoryFields
                .AsNoTracking()
                .Where(field => field.InventoryId == id)
                .Select(field => (int?)field.DisplayOrder)
                .MaxAsync() ?? 0;

            _dbContext.InventoryFields.Add(new InventoryField
            {
                Id = Guid.NewGuid(),
                InventoryId = id,
                FieldType = model.Form.FieldType,
                SlotNumber = model.Form.SlotNumber,
                Title = model.Form.Title.Trim(),
                Description = model.Form.Description?.Trim(),
                ShowInTable = model.Form.ShowInTable,
                DisplayOrder = nextOrder + 1
            });
        }
        else
        {
            var field = await _dbContext.InventoryFields
                .FirstOrDefaultAsync(existing => existing.Id == model.Form.Id && existing.InventoryId == id);
            if (field is null)
            {
                return NotFound();
            }

            field.FieldType = model.Form.FieldType;
            field.SlotNumber = model.Form.SlotNumber;
            field.Title = model.Form.Title.Trim();
            field.Description = model.Form.Description?.Trim();
            field.ShowInTable = model.Form.ShowInTable;
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Fields), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(Guid id, Guid fieldId)
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

        var field = await _dbContext.InventoryFields
            .FirstOrDefaultAsync(existing => existing.Id == fieldId && existing.InventoryId == id);
        if (field is not null)
        {
            _dbContext.InventoryFields.Remove(field);
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Fields), new { id });
    }

    [Authorize]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReorderFields(Guid id, [FromBody] IReadOnlyList<FieldReorderItemViewModel> fields)
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

        var fieldIds = fields.Select(field => field.Id).ToArray();
        var existingFields = await _dbContext.InventoryFields
            .Where(field => field.InventoryId == id && fieldIds.Contains(field.Id))
            .ToListAsync();

        var orderById = fields.ToDictionary(field => field.Id, field => field.DisplayOrder);
        foreach (var field in existingFields)
        {
            field.DisplayOrder = orderById[field.Id];
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
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

    private async Task<string?> ResolveImageUrlAsync(InventoryFormViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            return model.ImageUrl;
        }

        if (!_imageUploadService.CanUpload)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Cloudinary is not configured. Set CLOUDINARY_URL or Cloudinary credentials before uploading images.");
            return model.ImageUrl;
        }

        try
        {
            return await _imageUploadService.UploadInventoryImageAsync(model.ImageFile, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return model.ImageUrl;
        }
    }

    private async Task<string> GetTagStringAsync(Guid inventoryId)
    {
        var tags = await _dbContext.InventoryTags
            .AsNoTracking()
            .Where(inventoryTag => inventoryTag.InventoryId == inventoryId)
            .Join(_dbContext.Tags,
                inventoryTag => inventoryTag.TagId,
                tag => tag.Id,
                (_, tag) => tag.Name)
            .OrderBy(name => name)
            .ToListAsync();

        return string.Join(", ", tags);
    }

    private async Task UpdateInventoryTagsAsync(Guid inventoryId, IReadOnlyCollection<string> tagNames)
    {
        var currentLinks = await _dbContext.InventoryTags
            .Where(inventoryTag => inventoryTag.InventoryId == inventoryId)
            .ToListAsync();

        if (tagNames.Count == 0)
        {
            _dbContext.InventoryTags.RemoveRange(currentLinks);
            return;
        }

        var normalizedNames = tagNames
            .Select(NormalizeTag)
            .ToHashSet(StringComparer.Ordinal);

        var existingTags = await _dbContext.Tags
            .Where(tag => normalizedNames.Contains(tag.NormalizedName))
            .ToListAsync();

        foreach (var tagName in tagNames)
        {
            var normalizedName = NormalizeTag(tagName);
            if (existingTags.Any(tag => tag.NormalizedName == normalizedName))
            {
                continue;
            }

            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                Name = tagName,
                NormalizedName = normalizedName
            };
            existingTags.Add(tag);
            _dbContext.Tags.Add(tag);
        }

        var desiredTagIds = existingTags
            .Where(tag => normalizedNames.Contains(tag.NormalizedName))
            .Select(tag => tag.Id)
            .ToHashSet();

        _dbContext.InventoryTags.RemoveRange(currentLinks.Where(link => !desiredTagIds.Contains(link.TagId)));

        var currentTagIds = currentLinks.Select(link => link.TagId).ToHashSet();
        foreach (var tagId in desiredTagIds.Where(tagId => !currentTagIds.Contains(tagId)))
        {
            _dbContext.InventoryTags.Add(new InventoryTag
            {
                InventoryId = inventoryId,
                TagId = tagId
            });
        }
    }

    private static IReadOnlyList<string> ParseTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var parsed = TryParseTagifyJson(value) ?? value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parsed
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(InventoryLimits.MaxTags)
            .ToList();
    }

    private static IEnumerable<string>? TryParseTagifyJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return document.RootElement
                .EnumerateArray()
                .Select(element => element.TryGetProperty("value", out var property) ? property.GetString() : null)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag!)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }

    private static IReadOnlyList<ApplicationUser> SortUsers(IReadOnlyList<ApplicationUser> users, string? sort)
    {
        return NormalizeAccessSort(sort) == "email"
            ? users.OrderBy(user => user.Email).ThenBy(user => user.UserName).ToList()
            : users.OrderBy(user => user.UserName).ThenBy(user => user.Email).ToList();
    }

    private static string NormalizeAccessSort(string? sort)
    {
        return string.Equals(sort, "email", StringComparison.OrdinalIgnoreCase) ? "email" : "name";
    }

    private async Task<InventoryFieldsViewModel> BuildFieldsViewModelAsync(Inventory inventory, InventoryFieldFormViewModel? form = null)
    {
        var fields = await _dbContext.InventoryFields
            .AsNoTracking()
            .Where(field => field.InventoryId == inventory.Id)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldType)
            .ThenBy(field => field.SlotNumber)
            .ToListAsync();

        return new InventoryFieldsViewModel
        {
            InventoryId = inventory.Id,
            InventoryTitle = inventory.Title,
            Fields = fields,
            Form = form ?? new InventoryFieldFormViewModel
            {
                InventoryId = inventory.Id,
                SlotNumber = FindFirstAvailableSlot(fields, InventoryFieldType.Text)
            }
        };
    }

    private static int FindFirstAvailableSlot(IReadOnlyList<InventoryField> fields, InventoryFieldType fieldType)
    {
        var usedSlots = fields
            .Where(field => field.FieldType == fieldType)
            .Select(field => field.SlotNumber)
            .ToHashSet();

        for (var slot = 1; slot <= InventoryLimits.MaxFieldsPerType; slot++)
        {
            if (!usedSlots.Contains(slot))
            {
                return slot;
            }
        }

        return InventoryLimits.MaxFieldsPerType;
    }

    private async Task<IReadOnlyList<NumericFieldStatsViewModel>> BuildNumericStatsAsync(Guid inventoryId, IReadOnlyList<InventoryField> fields)
    {
        var items = _dbContext.Items
            .AsNoTracking()
            .Where(item => item.InventoryId == inventoryId);

        var configuredNumberFields = fields
            .Where(field => field.FieldType == InventoryFieldType.Number)
            .ToDictionary(field => field.SlotNumber);

        return new[]
        {
            await BuildNumericSlotStatsAsync(items, configuredNumberFields, 1),
            await BuildNumericSlotStatsAsync(items, configuredNumberFields, 2),
            await BuildNumericSlotStatsAsync(items, configuredNumberFields, 3)
        };
    }

    private static async Task<NumericFieldStatsViewModel> BuildNumericSlotStatsAsync(
        IQueryable<Item> items,
        IReadOnlyDictionary<int, InventoryField> configuredFields,
        int slotNumber)
    {
        var values = slotNumber switch
        {
            1 => items.Where(item => item.Num1.HasValue).Select(item => item.Num1!.Value),
            2 => items.Where(item => item.Num2.HasValue).Select(item => item.Num2!.Value),
            3 => items.Where(item => item.Num3.HasValue).Select(item => item.Num3!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(slotNumber), slotNumber, "Unsupported numeric field slot.")
        };

        var filledCount = await values.CountAsync();
        return new NumericFieldStatsViewModel
        {
            Label = configuredFields.TryGetValue(slotNumber, out var field) ? field.Title : $"Number {slotNumber}",
            FilledCount = filledCount,
            Average = filledCount == 0 ? null : await values.AverageAsync(),
            Minimum = filledCount == 0 ? null : await values.MinAsync(),
            Maximum = filledCount == 0 ? null : await values.MaxAsync()
        };
    }
}
