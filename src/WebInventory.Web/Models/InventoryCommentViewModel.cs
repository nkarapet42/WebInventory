namespace WebInventory.Web.Models;

public class InventoryCommentViewModel
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string BodyHtml { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentViewModel
{
    public Guid InventoryId { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;
}
