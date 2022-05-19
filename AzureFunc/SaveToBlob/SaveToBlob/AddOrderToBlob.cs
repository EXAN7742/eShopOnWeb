using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Text;

namespace SaveToBlob
{
    public static class AddOrderToBlob
    {
        [FunctionName("AddOrderToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            string data = requestBody;

            if (string.IsNullOrEmpty(data))
                data = $"{{Time:{DateTime.Now.ToString()}}}";

            
                string name;

                name = Guid.NewGuid().ToString("n");

                await CreateBlob(name + ".json", data);
            


            return new OkObjectResult("");
        }

        private async static Task CreateBlob(string name, string data)
        {
            string connectionString;

            connectionString = "DefaultEndpointsProtocol=https;AccountName=exan;AccountKey=iVT3X+KRH3qdaedrSqmhI56SmC8a1391XITD8Mnl+JEoXgKPE1mQ5PKmmpm0Fv06iiSoN8H60oOF+AStPZ7j/A==;EndpointSuffix=core.windows.net";


            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orders");

            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(name);

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                await blobClient.UploadAsync(ms);
            }
        }
    }
}
