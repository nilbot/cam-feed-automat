using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Feeder.Model;

namespace Feeder.Configuration
{
    public class BeckhoffConfig
    {
        public string BeckhoffConnectionId { get; set; }
        public int BeckhoffConnectionPort { get; set; }
        public string[] BeckhoffVariables { get; set; }
    }

    public class CameraDictionary
    {
        public Dictionary<Guid, string> CameraNameTable { get; set; }
        public Dictionary<string, Guid> BeckhoffVariableTable { get; set; }
    }

    public class CameraParameter
    {
        public CameraParameter(CLEyeMulticamAPI.CLEyeCameraColorMode clEyeCameraColorMode,
                               CLEyeMulticamAPI.CLEyeCameraResolution clEyeCameraResolution, int i, int prebuffer, int postbuffer)
        {
            ColorMode = clEyeCameraColorMode;
            Resolution = clEyeCameraResolution;
            FrameRate = i;
            PreBufferTimeInSeconds = prebuffer;
            PostBufferTimeInSeconds = postbuffer;
        }

        private static PixelFormat matchColorMode(CLEyeMulticamAPI.CLEyeCameraColorMode colorMode)
        {
            switch (colorMode)
            {
                case CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_BAYER_RAW:
                case CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_MONO_PROCESSED:
                case CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_MONO_RAW:
                    return PixelFormat.Format8bppIndexed; //TODO strife is correct but video can not be decoded, is there a special encoder scheme for mono video? Third party library bug?
                case CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED:
                case CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_RAW:
                    return PixelFormat.Format32bppRgb;
            }
            return PixelFormat.DontCare;
        }

        public CLEyeMulticamAPI.CLEyeCameraColorMode ColorMode { get; set; }
        public CLEyeMulticamAPI.CLEyeCameraResolution Resolution { get; set; }
        public int FrameRate { get; set; }
        public PixelFormat ColorFormat { get { return matchColorMode(ColorMode); } }
        public int PreBufferTimeInSeconds { get; set; }
        public int PostBufferTimeInSeconds { get; set; }
    }
}
