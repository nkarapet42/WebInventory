using System.ComponentModel.DataAnnotations;
using WebInventory.Domain.Enums;

namespace WebInventory.Web.Models;

public class InventoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? DescriptionMarkdown { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(500)]
    [Url]
    public string? ImageUrl { get; set; }

    [Display(Name = "Upload image")]
    public IFormFile? ImageFile { get; set; }

    [StringLength(1000)]
    public string? Tags { get; set; }

    public InventoryAccessMode AccessMode { get; set; }

    public uint? RowVersion { get; set; }
}
