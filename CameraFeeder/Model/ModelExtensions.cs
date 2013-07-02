using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AviFile;
using Feeder.Properties;
using Feeder.Configuration;

namespace Feeder.Model
{
    public static class ModelExtensions
    {
        public static AviManager CreateVideoStream(this CameraMonitor cameraMonitor)
        {
            var _dtfi = new DateTimeFormatInfo { DateSeparator = "_", TimeSeparator = "_" };
            var _aviManager =
                new AviManager(
                    Environment.ExpandEnvironmentVariables(Resources.UserDataPath) + @"\RecordingFromCamID_" +
                    cameraMonitor.CameraName + @"_" + DateTime.UtcNow.ToString(_dtfi) + @".avi", false);

            //var _aviStream = _aviManager.AddVideoStream(false, 10,
            //                                            (int)cameraMonitor.Device.FrameDimension.Size,
            //                                            cameraMonitor.Width, cameraMonitor.Height,
            //                                            cameraMonitor.Device.Parameter.ColorFormat);


            //return new Tuple<AviManager, VideoStream>(_aviManager, _aviStream);
            return _aviManager;
        }
    }
}
