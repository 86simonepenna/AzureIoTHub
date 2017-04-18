using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace AzureIoTHub.UWP.Service
{
    class PirSensorService : IDisposable
    {
        public enum SensorType
        {
            ActiveHigh,
            ActiveLow
        }

        private GpioPin pirSensorPin;
        private GpioPinEdge pirSensorEdge;
        public event EventHandler<GpioPinValueChangedEventArgs> MotionDetected;

        public double DebounceTimeout
        {
            get { return pirSensorPin.DebounceTimeout.TotalMilliseconds; }
        }

        public PirSensorService(int sensorPin, SensorType sensorType, int debounceTimeout = 1000)
        {
            var gpioController = GpioController.GetDefault();
            if (gpioController != null)
            {
                pirSensorEdge = sensorType == SensorType.ActiveLow ? GpioPinEdge.FallingEdge : GpioPinEdge.RisingEdge;
                pirSensorPin = gpioController.OpenPin(sensorPin);
                pirSensorPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                pirSensorPin.DebounceTimeout = TimeSpan.FromMilliseconds(debounceTimeout);
                pirSensorPin.ValueChanged += PirSensorPin_ValueChanged;
            }
            else
            {
                Debug.WriteLine("Error: GPIO controller not found.");
            }
        }

        public void AddThreshold()
        {
            if (pirSensorPin.DebounceTimeout.Seconds < 14)
            {
                pirSensorPin.DebounceTimeout = pirSensorPin.DebounceTimeout.Add(TimeSpan.FromMilliseconds(1000));
            }
        }

        public void SubThreshold()
        {
            if (pirSensorPin.DebounceTimeout.Seconds > 1)
            {
                pirSensorPin.DebounceTimeout = pirSensorPin.DebounceTimeout.Subtract(TimeSpan.FromMilliseconds(1000));
            }
        }

        public void Dispose()
        {
            pirSensorPin.Dispose();
        }

        private void PirSensorPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (MotionDetected != null && args.Edge == pirSensorEdge)
            {
                MotionDetected(this, args);
            }
        }
    }
}
