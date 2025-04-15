using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests.Models
{
    public class Item
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string Text { get; set; }
    }
}