using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text;
using System.IO;

namespace OrderItemsReserver
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static void Run([ServiceBusTrigger("orders", Connection = "eShopSB")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            log.LogInformation($"Exceptionnn");
            //throw new NotImplementedException();

            string data = myQueueItem;

            if (string.IsNullOrEmpty(data))
                data = $"{{Time:{DateTime.Now.ToString()}}}";

            string name;

            name = Guid.NewGuid().ToString("n");

            CreateBlob(name + ".json", data);
        }

        private static void CreateBlob(string name, string data)
        {
            string connectionString;

            connectionString = "DefaultEndpointsProtocol=https;AccountName=eshop01storageaccount;AccountKey=ARt5JKe1JaMGIozZJciM+Ny6kOnsu/5NmvqEFI8NuheQl0ktscAsAeiSvBzd5rNATLNSUU9ZFPRD+AStTVRJSA==;EndpointSuffix=core.windows.net";


            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orders");

            containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(name);

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                blobClient.Upload(ms);
            }
        }
    }
}
