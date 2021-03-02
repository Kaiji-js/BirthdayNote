using BirthdayBot.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirthdayBot.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        // 本Botで使用するCosmosDBのコンテナを取得
        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string contairName)
        {
            this.container = cosmosClient.GetContainer(databaseName, contairName);
        }

        // データ追加
        public async Task AddItemAsync(Item item)
        {
            await this.container.CreateItemAsync<Item>(item, new PartitionKey(item.Id));
        }

        // データ削除
        public async Task<bool> DeleteItemAsync(string queryString)
        {
            var results = (List<Item>)GetItemsAsync(queryString).Result;
            var isComplete = false;

            if (results.Count != 0)
            {
                foreach (var item in results)
                {
                    await this.container.DeleteItemAsync<Item>(item.Id, new PartitionKey(item.Id));
                }

                isComplete = true;
            }

            return isComplete;
        }

        // データ取得(1件)
        public async Task<Item> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Item> response = await this.container.ReadItemAsync<Item>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch(CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        // データ取得(クエリに基づき取得)
        public async Task<IEnumerable<Item>> GetItemsAsync(string queryString)
        {
            var query = this.container.GetItemQueryIterator<Item>(new QueryDefinition(queryString));
            List<Item> results = new List<Item>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        // データ更新
        public async Task UpdateItemAsync(string id, Item item)
        {
            await this.container.UpsertItemAsync<Item>(item, new PartitionKey(id));
        }
    }
}
