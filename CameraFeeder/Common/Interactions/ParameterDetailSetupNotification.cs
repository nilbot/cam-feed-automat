using System.Collections;
using Feeder.Configuration;
using Feeder.Model;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace Feeder.Common.Interactions
{
    public class ParameterDetailSetupNotification : Confirmation
    {
        public ParameterDetailSetupNotification()
        {
            Target = new CameraParameter(CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED, CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_QVGA, 50, 10, 1);
        }


        public IEnumerable DetailKeys
        {
            get { return Target.GetDetailKeys(); }
        }

        public CameraParameter Target { get; set; }
    }

    public static class CameraParameterExtension
    {
        public static IEnumerable GetDetailKeys(this CameraParameter p)
        {
            return new[] { "ColorMode", "FrameRate", "Resolution", "PreBufferTimeInSeconds", "PostBufferTimeInSeconds" };
        }
    }
}
