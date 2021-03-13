using BirthdayBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BirthdayBot.Services
{
    /// <summary>
    /// CosmosDBサービスのインターフェース
    /// </summary>
    public interface ICosmosDbService
    {
        Task<IEnumerable<Item>> GetItemsAsync(string query);

        Task<Item> GetItemAsync(string id);

        Task AddItemAsync(Item item);

        Task UpdateItemAsync(string id, Item item);

        Task<bool> DeleteItemAsync(string queryString);
    }
}
