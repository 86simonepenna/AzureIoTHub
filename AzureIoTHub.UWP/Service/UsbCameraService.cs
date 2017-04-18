using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AzureIoTHub.UWP.Service
{
    class UsbCameraService : IDisposable
    {
        private MediaCapture mediaCapture;
        private bool isInitialized = false;

        public MediaCapture MediaCaptureInstance
        {
            get { return mediaCapture; }
        }

        public async Task<bool> InitializeAsync()
        {
            if (mediaCapture == null)
            {
                var cameraDevice = await FindCameraDevice();

                if (cameraDevice == null)
                {
                    isInitialized = false;
                    return false;
                }

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                mediaCapture = new MediaCapture();
                try
                {
                    await mediaCapture.InitializeAsync(settings);
                    isInitialized = true;
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine("UsbCamera: UnauthorizedAccessException: " + ex.ToString() + "Ensure webcam capability is added in the manifest.");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UsbCamera: Exception when initializing MediaCapture:" + ex.ToString());
                    throw;
                }
            }
            return false;
        }

        public async void StartCameraPreview()
        {
            try
            {
                await mediaCapture.StartPreviewAsync();
            }
            catch
            {
                Debug.WriteLine("UsbCamera: Failed to start camera preview stream");
                throw;
            }
        }


        public async Task<StorageFile> CapturePhoto()
        {
            StorageFile file = null;

            string fileName = GenerateNewFileName() + ".jpg";
            CreationCollisionOption collisionOption = CreationCollisionOption.GenerateUniqueName;
            file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, collisionOption);

            await mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);

            return file;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public async void StopCameraPreview()
        {
            try
            {
                await mediaCapture.StopPreviewAsync();
            }
            catch
            {
                Debug.WriteLine("UsbCamera: Failed to stop camera preview stream");
                throw;
            }
        }

        public void Dispose()
        {
            mediaCapture?.Dispose();
            isInitialized = false;
        }


        private static async Task<DeviceInformation> FindCameraDevice()
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return allVideoDevices.Count > 0 ? allVideoDevices[0] : null;
        }

        private string GenerateNewFileName(string prefix = "IMG_")
        {
            return prefix + "_" + DateTime.UtcNow.ToString("yyyy-MMM-dd_HH-mm-ss");
        }
    }
}