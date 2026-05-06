using System.Security.Claims;
using WebInventory.Application.Models;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Identity;

namespace WebInventory.Application.Interfaces;

public interface IAccessControlService
{
    Task<bool> CanWriteAsync(Inventory inventory, ClaimsPrincipal user);
    Task<IReadOnlyList<ApplicationUser>> GetWritersAsync(Guid inventoryId);
    Task<AccessChangeResult> AddWriterAsync(Guid inventoryId, string userIdentifier);
    Task<AccessChangeResult> RemoveWriterAsync(Guid inventoryId, string userId);
}
