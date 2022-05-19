using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;


namespace ReadQueueToBlob
{
    public class ReadQueue
    {
        [FunctionName("ReadQueue")]
        public void Run([QueueTrigger("orders", Connection = "StorageConnectionAppSetting")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
