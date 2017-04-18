using Microsoft.AspNet.SignalR.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AzureIoTHub.WebJob
{
    public class BlobListener : IDisposable
    {
        private string _connectionString;
        private string _hubUrl;
        private HubConnection _hubConnection;
        private IHubProxy _blobHubProxy;
        private ServiceClient _serviceClient;

        public BlobListener(string connectionString, string webAppUrl)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(webAppUrl))
                throw new ArgumentNullException(nameof(webAppUrl));

            _connectionString = connectionString;
            _hubUrl = webAppUrl;
            _serviceClient = ServiceClient.CreateFromConnectionString(connectionString, TransportType.Amqp);
            _hubConnection = new HubConnection(_hubUrl);
            _blobHubProxy = _hubConnection.CreateHubProxy("BlobHub");
            _hubConnection.Start().Wait();
        }

        public async Task StartListenAsync()
        {
            try
            {
                FileNotificationReceiver<FileNotification> fileNotificationReceiver = _serviceClient.GetFileNotificationReceiver();
                while (true)
                {
                    FileNotification fileNotification = null;
                    try
                    {
                        fileNotification = await fileNotificationReceiver.ReceiveAsync();
                        if (fileNotification != null)
                        {
                            await SendToWebAppAsync(fileNotification);
                            await fileNotificationReceiver.CompleteAsync(fileNotification);
                        }
                    }
                    catch (Exception ex)
                    {
                        await fileNotificationReceiver.AbandonAsync(fileNotification);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task SendToWebAppAsync(FileNotification fileNotification)
        {
            if (fileNotification != null)
            {
                try
                {
                    await _blobHubProxy.Invoke("BlobIsLoad", fileNotification);
                    Console.WriteLine("Send Blob To WebApp" + fileNotification.BlobName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Send Blob To WebApp ERR: " + ex.Message);
                    throw;
                }

            }
        }

        public void Dispose()
        {
            _hubConnection.Stop();
        }
    }


}
