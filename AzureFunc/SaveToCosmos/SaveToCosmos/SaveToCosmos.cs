using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

namespace SaveToCosmos;

public static class SaveToCosmos
{
    private static readonly string _endpointUri = "https://eshop01.documents.azure.com:443/";
    private static readonly string _primaryKey = "PnjJdu4LO5r5WpQ9o3ShjEL95GIcYoh5vLrI9p4OG9Y2oBRdClztnzC5Djehng0bUd0BB0j39puyHWlRXUsDXA==";

    [FunctionName("OrderToCosmos")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //dynamic data = JsonConvert.DeserializeObject(requestBody);
        Order order = JsonConvert.DeserializeObject<Order>(requestBody);
        
        //if (string.IsNullOrEmpty(data))
        //    data = $"{{Time:{DateTime.Now.ToString()}}}";


        string name;

        name = Guid.NewGuid().ToString("n");

        await CreateCosmos(order);



        return new OkObjectResult("");
    }

    private async static Task CreateCosmos(Order order)
    {
        using (CosmosClient client = new CosmosClient(_endpointUri, _primaryKey))
        {
            DatabaseResponse databaseResponse = await client.CreateDatabaseIfNotExistsAsync("Orders");
            Database targetDatabase = databaseResponse.Database;
            
            IndexingPolicy indexingPolicy = new IndexingPolicy
            {
                IndexingMode = IndexingMode.Consistent,
                Automatic = true,
                IncludedPaths =
                {
                    new IncludedPath
                    {
                        Path = "/*"
                    }
                }
            };
            var containerProperties = new ContainerProperties("Order", "/BuyerId")
            {
                IndexingPolicy = indexingPolicy
            };
            var containerResponse = await targetDatabase.CreateContainerIfNotExistsAsync(containerProperties, 1000);
            var customContainer = containerResponse.Container;

            ItemResponse<Order> cosmosOrder = await customContainer.CreateItemAsync<Order>(order, new PartitionKey(order.BuyerId));
        }
    }
}
