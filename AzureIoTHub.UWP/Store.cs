using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureIoTHub.UWP
{
    class Store
    {
        public bool IsPirEnable { get; set; }
        public bool IsDistanceSensorEnable { get; set; }
                public string IotHubConnectionString { get; set; }
        public string IotHubRegistryConnectionString { get; set; }
        
        private static Store instance;

        public static Store Instance
        {
            get
            {
                instance = Read();
                return instance;
            }
        }

        public static void SaveData()
        {
            File.WriteAllText("Store.json", JsonConvert.SerializeObject(Instance));
        }

        private static Store Read()
        {
            var data = File.ReadAllText("Store.json");
            return JsonConvert.DeserializeObject<Store>(data);
        }
    }
}
