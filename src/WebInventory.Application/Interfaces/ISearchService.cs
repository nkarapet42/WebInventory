using WebInventory.Application.Models;

namespace WebInventory.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string term, int limit = 20);
}
