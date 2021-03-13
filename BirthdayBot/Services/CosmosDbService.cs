using BirthdayBot.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirthdayBot.Services
{
    /// <summary>
    /// CosmosDBサービスのCRUD処理
    /// </summary>
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string contairName)
        {
            // CosmosDBのコンテナを設定
            this.container = cosmosClient.GetContainer(databaseName, contairName);
        }

        /// <summary>
        /// データ追加
        /// </summary>
        public async Task AddItemAsync(Item item)
        {
            await this.container.CreateItemAsync<Item>(item, new PartitionKey(item.Id));
        }

        /// <summary>
        /// データ削除
        /// </summary>
        public async Task<bool> DeleteItemAsync(string queryString)
        {
            var results = (List<Item>)this.GetItemsAsync(queryString).Result;
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

        /// <summary>
        /// データ取得(1件)
        /// </summary>
        public async Task<Item> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Item> response = await this.container.ReadItemAsync<Item>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// データ取得(クエリに基づき取得)
        /// </summary>
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

        /// <summary>
        /// データ更新
        /// </summary>
        public async Task UpdateItemAsync(string id, Item item)
        {
            await this.container.UpsertItemAsync<Item>(item, new PartitionKey(id));
        }
    }
}
