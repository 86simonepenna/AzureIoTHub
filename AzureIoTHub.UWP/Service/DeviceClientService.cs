using AzureIoTHub.UWP;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace AzureIoTHub.UWP.Service
{
    class DeviceClientService
    {
        DeviceClient deviceClient;
        Microsoft.Azure.Devices.Device device;
        Microsoft.Azure.Devices.RegistryManager registryManager;
        TwinCollection twinCollection = new TwinCollection();

        public event EventHandler NotificationBlobEvent;
        public event EventHandler StartPirEvent;
        public event EventHandler StopPirEvent;
        public event EventHandler AddThresholdEvent;
        public event EventHandler SubThresholdEvent;
        public event EventHandler InfoTextEvent;


        public DeviceClientService()
        {

            deviceClient = DeviceClient.CreateFromConnectionString(Store.Instance.IotHubConnectionString, GetDeviceID(), TransportType.Mqtt);
            deviceClient.OpenAsync().Wait();

            ReceiveMessageAsync();

            deviceClient.SetMethodHandlerAsync("StartPir", StartPir, null);
            deviceClient.SetMethodHandlerAsync("StopPir", StopPir, null);
            deviceClient.SetMethodHandlerAsync("AddThreshold", AddThreshold, null);
            deviceClient.SetMethodHandlerAsync("SubThreshold", SubThreshold, null);

            deviceClient.SetDesiredPropertyUpdateCallback(OnDesiredPropertyChanged, null).Wait();
        }


        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            InfoTextEvent(desiredProperties["InfoText"], null);
        }


        public async Task UpdateReportedPropertiesAsync(string key, string value)
        {
            twinCollection[key] = value;
            await deviceClient.UpdateReportedPropertiesAsync(twinCollection);
        }

        public async Task DeviceRegistryAsync()
        {
            registryManager = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(Store.Instance.IotHubRegistryConnectionString);
            device = await registryManager.GetDeviceAsync(GetDeviceID());
            if (device == null)
                device = await registryManager.AddDeviceAsync(new Microsoft.Azure.Devices.Device(GetDeviceID()));
        }

        private Task<MethodResponse> StartPir(MethodRequest methodRequest, object userContext)
        {
            StartPirEvent(userContext, null);
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private Task<MethodResponse> StopPir(MethodRequest methodRequest, object userContext)
        {
            StopPirEvent(userContext, null);
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private Task<MethodResponse> AddThreshold(MethodRequest methodRequest, object userContext)
        {
            AddThresholdEvent(userContext, null);
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private Task<MethodResponse> SubThreshold(MethodRequest methodRequest, object userContext)
        {
            SubThresholdEvent(userContext, null);
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        public async Task UploadBlob(StorageFile storageFile)
        {
            Stream stream = await storageFile.OpenStreamForReadAsync();

            try
            {
                await deviceClient.UploadToBlobAsync(storageFile.Name, stream);
            }
            catch (Exception ex)
            {
                throw;
            }

            await storageFile.DeleteAsync();

            stream.Dispose();
        }


        private void ReceiveMessageAsync()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    Message message = await deviceClient.ReceiveAsync();

                    if (message == null)
                        continue;

                    MessageFromCloud messageFromCloud = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageFromCloud>(Encoding.ASCII.GetString(message.GetBytes()));


                    switch (messageFromCloud.Action)
                    {
                        case Action.BLOB_UPLOADED:
                            NotificationBlob(messageFromCloud);
                            break;
                        default:
                            break;
                    }

                    await deviceClient.CompleteAsync(message);
                }
            });
        }

        private void NotificationBlob(MessageFromCloud messageFromCloud)
        {
            NotificationBlobEvent(messageFromCloud, null);
        }

        private static string GetDeviceID()
        {
            foreach (Windows.Networking.HostName name in Windows.Networking.Connectivity.NetworkInformation.GetHostNames())
            {
                if (Windows.Networking.HostNameType.DomainName == name.Type)
                {
                    return name.DisplayName;
                }
            }
            return null;
        }
        
        public enum Action
        {
            BLOB_UPLOADED
        }

        public class MessageFromCloud
        {
            public Action Action { get; }
            public string TextMessage { get; }
            public DateTimeOffset SendMessageDate { get; }
        }
        
    }
}