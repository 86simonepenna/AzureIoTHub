using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureIoTHub.Web.Models
{
    public class DevicesViewModel
    {
        public long DevicesConnected { get; set; }
        public List<DeviceViewModel> Devices { get; set; } = new List<DeviceViewModel>();
    }

    public class DeviceViewModel
    {
        public string DeviceId { get; set; }     
        public int Threshold { get; set; }
    }
}