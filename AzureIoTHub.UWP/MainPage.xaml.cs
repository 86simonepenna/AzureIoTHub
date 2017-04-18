using AzureIoTHub.UWP.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AzureIoTHub.UWP
{

    public sealed partial class MainPage : Page
    {
        DeviceClientService deviceClientService;
        UsbCameraService usbCameraService;
        PirSensorService pirSensorService;

        GpioPin pinUploadingPhoto;
        GpioPin pinPhotoArrived;
        GpioPin pinPirEnable;

        const int PIN_PIR_SENSOR = 5;
        const int PIN_UPLOADING_PHOTO = 6;
        const int PIN_PHOTO_ARRIVED = 26;
        const int PIN_PIR_ENABLE = 19;

        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush yellowBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.Green);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private SolidColorBrush blueBrush = new SolidColorBrush(Windows.UI.Colors.Blue);

        public MainPage()
        {
            this.InitializeComponent();

            Task.Run(async () =>
            {
                deviceClientService = new DeviceClientService();
                deviceClientService.NotificationBlobEvent += DeviceClientService_NotificationBlobArrived;
                deviceClientService.StartPirEvent += DeviceClientService_StartPirEvent;
                deviceClientService.StopPirEvent += DeviceClientService_StopPirEvent;
                deviceClientService.AddThresholdEvent += DeviceClientService_AddThresholdEvent;
                deviceClientService.SubThresholdEvent += DeviceClientService_SubThresholdEvent;
                deviceClientService.InfoTextEvent += DeviceClientService_InfoTextEvent;

                usbCameraService = new UsbCameraService();

                var isInitialized = await usbCameraService.InitializeAsync();

                if (isInitialized)
                {
                    pirSensorService = new PirSensorService(PIN_PIR_SENSOR, PirSensorService.SensorType.ActiveHigh);
                    pirSensorService.MotionDetected += PirSensorService_MotionDetected;
                    await deviceClientService.UpdateReportedPropertiesAsync("CurrentThreshold", pirSensorService.DebounceTimeout.ToString());

                    pinUploadingPhoto = GpioController.GetDefault().OpenPin(PIN_UPLOADING_PHOTO);
                    pinUploadingPhoto.SetDriveMode(GpioPinDriveMode.Output);
                    pinUploadingPhoto.Write(GpioPinValue.Low);

                    pinPhotoArrived = GpioController.GetDefault().OpenPin(PIN_PHOTO_ARRIVED);
                    pinPhotoArrived.SetDriveMode(GpioPinDriveMode.Output);
                    pinPhotoArrived.Write(GpioPinValue.Low);

                    pinPirEnable = GpioController.GetDefault().OpenPin(PIN_PIR_ENABLE);
                    pinPirEnable.SetDriveMode(GpioPinDriveMode.Output);
                    pinPirEnable.Write(GpioPinValue.High);
                }


            }).Wait();

            Label_Threshold.Text = $"Threshold: {pirSensorService.DebounceTimeout} milliseconds";

        }

        private async void DeviceClientService_InfoTextEvent(object sender, EventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Label_InfoText.Text = $"Info Text: {sender.ToString()}";
            });

        }

        private async void DeviceClientService_AddThresholdEvent(object sender, EventArgs e)
        {
            pirSensorService.AddThreshold();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
             () =>
             {
                 Label_Threshold.Text = $"Threshold: {pirSensorService.DebounceTimeout} milliseconds";
             });

            await deviceClientService.UpdateReportedPropertiesAsync("CurrentThreshold", pirSensorService.DebounceTimeout.ToString());
        }

        private async void DeviceClientService_SubThresholdEvent(object sender, EventArgs e)
        {
            pirSensorService.SubThreshold();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   Label_Threshold.Text = $"Threshold: {pirSensorService.DebounceTimeout} milliseconds";
               });

            await deviceClientService.UpdateReportedPropertiesAsync("CurrentThreshold", pirSensorService.DebounceTimeout.ToString());
        }

        private async void DeviceClientService_StartPirEvent(object sender, EventArgs e)
        {
            pirSensorService.MotionDetected -= PirSensorService_MotionDetected;
            pirSensorService.MotionDetected += PirSensorService_MotionDetected;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Ellipse_pirEnabled.Fill = blueBrush;
            });

            pinPirEnable.Write(GpioPinValue.High);
        }

        private async void DeviceClientService_StopPirEvent(object sender, EventArgs e)
        {
            pirSensorService.MotionDetected -= PirSensorService_MotionDetected;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
           () =>
           {
               Ellipse_pirEnabled.Fill = grayBrush;
           });
            pinPirEnable.Write(GpioPinValue.Low);
        }

        private async void DeviceClientService_NotificationBlobArrived(object sender, EventArgs e)
        {
            pinPhotoArrived.Write(GpioPinValue.High);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
             () =>
             {
                 Ellipse_photoArrived.Fill = greenBrush;
             });

            Task.Delay(3000).Wait();

            pinPhotoArrived.Write(GpioPinValue.Low);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
             () =>
             {
                 Ellipse_photoArrived.Fill = grayBrush;
             });
        }

        private async void PirSensorService_MotionDetected(object sender, Windows.Devices.Gpio.GpioPinValueChangedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                 () =>
                 {
                     Ellipse_pir.Fill = redBrush;
                 });


            StorageFile photo = await usbCameraService.CapturePhoto();

            pinUploadingPhoto.Write(GpioPinValue.High);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
              () =>
              {
                  Ellipse_upload.Fill = yellowBrush;
              });

            await deviceClientService.UploadBlob(photo);


            pinUploadingPhoto.Write(GpioPinValue.Low);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Ellipse_upload.Fill = grayBrush;
                    Ellipse_pir.Fill = grayBrush;
                });

        }
    }
}
