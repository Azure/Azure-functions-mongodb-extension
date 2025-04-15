using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(CosmosDBMongoWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddCosmosDBMongo();
        }
    }
}