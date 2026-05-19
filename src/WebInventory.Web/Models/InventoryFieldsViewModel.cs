using System.ComponentModel.DataAnnotations;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;

namespace WebInventory.Web.Models;

public class InventoryFieldsViewModel
{
    public required Guid InventoryId { get; set; }
    public required string InventoryTitle { get; set; }
    public IReadOnlyList<InventoryField> Fields { get; set; } = Array.Empty<InventoryField>();
    public InventoryFieldFormViewModel Form { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class InventoryFieldFormViewModel
{
    public Guid? Id { get; set; }
    public Guid InventoryId { get; set; }

    [Required]
    public InventoryFieldType FieldType { get; set; }

    [Range(1, 3)]
    public int SlotNumber { get; set; } = 1;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    public bool ShowInTable { get; set; } = true;
}

public class FieldReorderItemViewModel
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
}
