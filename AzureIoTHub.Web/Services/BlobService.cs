using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AzureIoTHub.Web.Services
{
    public interface IBlobService
    {
        Task<byte[]> GetPhotoAsync(string blobName);
    }

    public class BlobService : IBlobService
    {
        private CloudBlobContainer _container;
        public BlobService(string connectionString, string containerName)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            if (!container.Exists())
                throw new InvalidOperationException("Container not exist");
            _container = container;            
        }

        public async Task<byte[]> GetPhotoAsync(string blobName)
        {
            try
            {
                MemoryStream imageMemory = new MemoryStream();
                ICloudBlob blob = await _container.GetBlobReferenceFromServerAsync(blobName);
                await blob.DownloadToStreamAsync(imageMemory);
                imageMemory.Position = 0;
                return imageMemory.ToArray();
            }
            catch(Exception ex)
            {
                var e = ex;
                return await Task.FromResult<byte[]>(new byte[] { });
            }
        }
    }
}