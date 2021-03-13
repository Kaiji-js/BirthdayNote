using Newtonsoft.Json;

namespace BirthdayBot.Models
{
    /// <summary>
    /// CosmosDBとのデータ送受信に使用するテーブル定義
    /// </summary>
    public class Item
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "birthday")]
        public string Birthday { get; set; }
    }
}
