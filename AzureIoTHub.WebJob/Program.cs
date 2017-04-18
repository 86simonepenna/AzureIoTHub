using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureIoTHub.WebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = ConfigurationManager.AppSettings["IotHubConnectionString"];
            string webAppUrl = ConfigurationManager.AppSettings["IoTHub.WebApp"];
            new BlobListener(connectionString, webAppUrl).StartListenAsync().Wait();
            Console.Read();
        }
    }
}
