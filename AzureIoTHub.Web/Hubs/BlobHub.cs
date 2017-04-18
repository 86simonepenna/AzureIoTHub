using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.Devices;
using AzureIoTHub.Web.Services;
using System.Threading.Tasks;
using Ninject;
using Newtonsoft.Json;

namespace AzureIoTHub.Web.Hubs
{
    public class BlobHub : Hub
    {
        private IServerIoTService _serverService;
        private IBlobService _blobService;
        public BlobHub()
        {
            _serverService = GlobalHost.DependencyResolver.Resolve<IServerIoTService>();
            _blobService = GlobalHost.DependencyResolver.Resolve<IBlobService>();           
        }
               
        public async Task BlobIsLoad(FileNotification fileBlob)
        {
            await _serverService.NotifyPhotoArrivedToDeviceAsync(fileBlob.DeviceId, fileBlob.BlobName);
            int threshold = await GetThresholdAsync(fileBlob.DeviceId);
            byte[] photo = await _blobService.GetPhotoAsync(fileBlob.BlobName);
            Clients.All.photoLoaded(fileBlob.DeviceId, threshold, photo);
        }

        private async Task<int> GetThresholdAsync(string deviceId)
        {
            var twinCollection = await _serverService.GetReportedPropertiesAsync(deviceId);
            int threshold = default(int);
            if (twinCollection != null)
            {
                Dictionary<string, object> reportedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(twinCollection.ToJson(Formatting.None));

                if (reportedProperties.Any(x => x.Key == "CurrentThreshold"))
                    threshold = int.Parse(reportedProperties["CurrentThreshold"].ToString());
            }
            return threshold;
        }
    }
}