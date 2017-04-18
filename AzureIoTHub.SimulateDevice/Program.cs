using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AzureIoTHub.SimulateDevice
{
    class Program : IDisposable
    {
        private static Timer _timerDevice = null;
        private static DeviceClient device;
        private static int sendCount = 0;
        static void Main(string[] args)
        {
            device = DeviceClient.CreateFromConnectionString(ConfigurationManager.AppSettings["IotHubConnectionString"], ConfigurationManager.AppSettings["DeviceID"]);
            device.OpenAsync().Wait();       
            _timerDevice = new Timer(10000);
            _timerDevice.Elapsed += SendDeviceImage;
            _timerDevice.Start();

            Task t = ReceiveMessageAsync();
            t.Wait();
            Console.Read();
        }

        private static void SendDeviceImage(object sender, ElapsedEventArgs e)
        {
            if (sendCount % 2 == 0)
            {
                device.UploadToBlobAsync("1.jpg", new MemoryStream(File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "1.JPG")))).Wait();
                Console.WriteLine("Write Blob: Andrea Tosato");
            }
            else
            {
                device.UploadToBlobAsync("2.jpg", new MemoryStream(File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "2.jpg")))).Wait();
                Console.WriteLine("Write Blob: Simone Penna");
            }
                

            sendCount += 1;
        }

        public void Dispose()
        {
            device.CloseAsync().Wait();
        }

        private static Task ReceiveMessageAsync()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    Message messaggioRicevuto = await device.ReceiveAsync();
                    if (messaggioRicevuto == null) continue;
                    
                    string messaggio = Encoding.ASCII.GetString(messaggioRicevuto.GetBytes());
                    Console.WriteLine(messaggio);
                    await device.CompleteAsync(messaggioRicevuto);
                }
            });
        }
    }
}
