using AzureIoTHub.Web.Hubs;
using AzureIoTHub.Web.Models;
using AzureIoTHub.Web.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace AzureIoTHub.Web.Controllers
{
    public class ServerController : Controller
    {
        private IServerIoTService _serverService;
        private IBlobService _blobService;
        public ServerController(IServerIoTService serverService, IBlobService blobService)
        {
            _serverService = serverService;
            _blobService = blobService;
        }

        public async Task<ActionResult> Index()
        {
            IEnumerable<string> devices = await _serverService.GetAllConnectedDevicesIdAsync();
            List<DeviceViewModel> devicesViewModel = new List<DeviceViewModel>();
            foreach(string deviceId in devices)
            {
                DeviceViewModel currenteDevice = new DeviceViewModel
                {
                    DeviceId = deviceId,
                    Threshold = await GetThresholdAsync(deviceId)
                };
                devicesViewModel.Add(currenteDevice);
            }
            
            DevicesViewModel model = new DevicesViewModel()
            {
                DevicesConnected = await _serverService.GetConnectedDeviceCountAsync(),
                Devices = devicesViewModel
            };
            return View(model);
        }

        #region [CloudToDevice]
        public async Task StartDevice(string deviceId)
        {
            try
            {
                await _serverService.StartDeviceListeningAsync(deviceId);
            }
            catch(Exception) { }
        }

        public async Task StopDevice(string deviceId)
        {
            try
            {
                await _serverService.StopDeviceListeningAsync(deviceId);
            }
            catch (Exception) { }
        }

        public async Task AddThresholdDevice(string deviceId)
        {
            try
            {
                await _serverService.AddThresholdDeviceAsync(deviceId);
            }
            catch (Exception) { }
        }

        public async Task SubThresholdDevice(string deviceId)
        {
            try
            {
                await _serverService.SubThresholdDeviceAsync(deviceId);
            }
            catch (Exception) { }
        }
        #endregion

        #region [Twin]
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

        public async Task SendTextToAllDevicesAsync(string text)
        {
            await _serverService.SendTextToAllDevicesAsync(text);
        }
        #endregion

        public class BlobData
        {

            public string DeviceID { get; set; }
            public string BlobName { get; set; }
        }

    }
}