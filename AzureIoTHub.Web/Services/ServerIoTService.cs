using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace AzureIoTHub.Web.Services
{
    public interface IServerIoTService
    {
        Task<long> GetConnectedDeviceCountAsync();
        Task<IEnumerable<string>> GetAllConnectedDevicesIdAsync();
        Task NotifyPhotoArrivedToDeviceAsync(string deviceId, string blobUri);
        Task SendTextToAllDevicesAsync(string text);
        Task<TwinCollection> GetReportedPropertiesAsync(string deviceId);
        Task<int> StartDeviceListeningAsync(string deviceId);
        Task<int> StopDeviceListeningAsync(string deviceId);
        Task<int> AddThresholdDeviceAsync(string deviceId);
        Task<int> SubThresholdDeviceAsync(string deviceId);
    }

    public class ServerIoTService : IServerIoTService
    {
        private ServiceClient _serviceClient;
        private RegistryManager _registryManager;
        private CloudToDeviceMethod startMethod = new CloudToDeviceMethod("StartPir", TimeSpan.FromSeconds(30));
        private CloudToDeviceMethod stopMethod = new CloudToDeviceMethod("StopPir", TimeSpan.FromSeconds(30));
        private CloudToDeviceMethod addThreshold = new CloudToDeviceMethod("AddThreshold", TimeSpan.FromSeconds(30));
        private CloudToDeviceMethod subThreshold = new CloudToDeviceMethod("SubThreshold", TimeSpan.FromSeconds(30));

        public ServerIoTService(string serviceConnectionString, string registryConnectionString, TransportType transport)
        {
            if (string.IsNullOrEmpty(serviceConnectionString)) throw new ArgumentNullException(nameof(serviceConnectionString));
            if (string.IsNullOrEmpty(registryConnectionString)) throw new ArgumentNullException(nameof(registryConnectionString));
            _serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString, transport);
            _registryManager = RegistryManager.CreateFromConnectionString(registryConnectionString);                  
        }
        
        public async Task<long> GetConnectedDeviceCountAsync()
        {
            var serviceStatistics = await _serviceClient.GetServiceStatisticsAsync();
            return serviceStatistics.ConnectedDeviceCount;
        }

        public async Task<IEnumerable<string>> GetAllConnectedDevicesIdAsync()
        {
            ConcurrentQueue<string> devicesId = new ConcurrentQueue<string>();
            var devices = await GetAllDevicesAsync();
            devices.AsParallel().ForAll(d =>
            {
                if (d.ConnectionState == DeviceConnectionState.Connected && d.Status == DeviceStatus.Enabled)
                    devicesId.Enqueue(d.Id);
            });
            return devicesId;
        }

        public async Task SendTextToAllDevicesAsync(string text)
        {
            try
            {
                IEnumerable<Device> devices = await GetAllDevicesAsync();
                foreach (Device d in devices.Where(x => x.ConnectionState == DeviceConnectionState.Connected))
                {
                    Twin oldTwin = await _registryManager.GetTwinAsync(d.Id);
                    Twin updateTwin = new Twin(d.Id)
                    {
                        ETag = oldTwin.ETag
                    };
                    updateTwin.Properties.Desired["InfoText"] = text;                    
                    Twin newTwin = await _registryManager.UpdateTwinAsync(updateTwin.DeviceId, updateTwin, updateTwin.ETag); // Require Owner connectionString                    
                }
            }
            catch(Exception){ }
        }
        
        public async Task<TwinCollection> GetReportedPropertiesAsync(string deviceId)
        {
            Twin t = await _registryManager.GetTwinAsync(deviceId); // Require Owner connectionString
            return t.Properties.Reported;
        }
        
        public async Task<int> StartDeviceListeningAsync(string deviceId)
        {
            CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(deviceId, startMethod);
            return result.Status;
        }

        public async Task<int> StopDeviceListeningAsync(string deviceId)
        {
            CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(deviceId, stopMethod);
            return result.Status;
        }

        public async Task<int> AddThresholdDeviceAsync(string deviceId)
        {
            CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(deviceId, addThreshold);
            return result.Status;
        }

        public async Task<int> SubThresholdDeviceAsync(string deviceId)
        {
            CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(deviceId, subThreshold);
            return result.Status;
        }

        public async Task NotifyPhotoArrivedToDeviceAsync(string deviceId, string blobUri)
        {
            MessageToDevice message = new MessageToDevice()
            {
                Action=Action.BLOB_UPLOADED,
                TextMessage = "Foto ricevuta con successo e presente al link: { blobUri}",
                SendMessageDate = DateTimeOffset.UtcNow
            };
            byte[] messagePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            await _serviceClient.SendAsync(deviceId, new Message(messagePayload)).ConfigureAwait(false);            
        }


        private async Task<IEnumerable<Device>> GetAllDevicesAsync()
        {
            return await _registryManager.GetDevicesAsync(int.MaxValue);
        }


        private enum Action {
            BLOB_UPLOADED
        }
        private class MessageToDevice
        {
            public Action Action { get; set; }
            public string TextMessage { get; set; }
            public DateTimeOffset SendMessageDate { get; set; }
        }
    }
}