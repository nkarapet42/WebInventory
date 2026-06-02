using System.ComponentModel.DataAnnotations;

namespace WebInventory.Web.Models;

public class SalesforceCustomerViewModel
{
    [Required]
    public required string UserId { get; set; }

    [Required]
    [StringLength(255)]
    [Display(Name = "Company name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(80)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(128)]
    [Display(Name = "Job title")]
    public string? JobTitle { get; set; }
}
