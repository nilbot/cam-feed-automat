using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Feeder.Common.Helpers;
using Feeder.Common.Timber;

namespace Feeder.Model
{
    public class CameraService : ICameraService<Guid>
    {
        #region ICameraService<Guid> Members

        public void GetAvailableDevices(Action<ObservableCollection<Guid>, Exception> callback)
        {
            try
            {
                var _deviceCount = CLEyeMulticamAPI.CLEyeGetCameraCount();

                var _devices = new ObservableCollection<Guid>();

                for (int _i = 0; _i != _deviceCount; _i++)
                    _devices.Add(CLEyeMulticamAPI.CLEyeGetCameraUUID(_i));

                callback(_devices, null);
            }
            catch (Exception _ex)
            {
                callback(null, _ex);
            }
        }

        #endregion
    }

    public static class CLEyeMulticamAPI
    {
        #region CLEyeCameraColorMode enum

        public enum CLEyeCameraColorMode
        {
            CLEYE_MONO_PROCESSED,
            CLEYE_COLOR_PROCESSED,
            CLEYE_MONO_RAW,
            CLEYE_COLOR_RAW,
            CLEYE_BAYER_RAW
        };

        #endregion

        #region CLEyeCameraParameter enum

        public enum CLEyeCameraParameter
        {
            // camera sensor parameters
            CLEYE_AUTO_GAIN, // [false, true]
            CLEYE_GAIN, // [0, 79]
            CLEYE_AUTO_EXPOSURE, // [false, true]
            CLEYE_EXPOSURE, // [0, 511]
            CLEYE_AUTO_WHITEBALANCE, // [false, true]
            CLEYE_WHITEBALANCE_RED, // [0, 255]
            CLEYE_WHITEBALANCE_GREEN, // [0, 255]
            CLEYE_WHITEBALANCE_BLUE, // [0, 255]
            // camera linear transform parameters
            CLEYE_HFLIP, // [false, true]
            CLEYE_VFLIP, // [false, true]
            CLEYE_HKEYSTONE, // [-500, 500]
            CLEYE_VKEYSTONE, // [-500, 500]
            CLEYE_XOFFSET, // [-500, 500]
            CLEYE_YOFFSET, // [-500, 500]
            CLEYE_ROTATION, // [-500, 500]
            CLEYE_ZOOM, // [-500, 500]
            // camera non-linear transform parameters
            CLEYE_LENSCORRECTION1, // [-500, 500]
            CLEYE_LENSCORRECTION2, // [-500, 500]
            CLEYE_LENSCORRECTION3, // [-500, 500]
            CLEYE_LENSBRIGHTNESS // [-500, 500]
        };

        #endregion

        #region CLEyeCameraResolution enum

        public enum CLEyeCameraResolution
        {
            CLEYE_QVGA,
            CLEYE_VGA
        };

        #endregion

        [DllImport("CLEyeMulticam.dll")]
        public static extern int CLEyeGetCameraCount();

        [DllImport("CLEyeMulticam.dll",CallingConvention=CallingConvention.Cdecl)]
        public static extern Guid CLEyeGetCameraUUID(int camId);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CLEyeCreateCamera(Guid camUUID, CLEyeCameraColorMode mode, CLEyeCameraResolution res,
                                                      float frameRate);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeDestroyCamera(IntPtr camera);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeCameraStart(IntPtr camera);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeCameraStop(IntPtr camera);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeCameraLED(IntPtr camera, bool on);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeSetCameraParameter(IntPtr camera, CLEyeCameraParameter param, int value);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CLEyeGetCameraParameter(IntPtr camera, CLEyeCameraParameter param);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeCameraGetFrameDimensions(IntPtr camera, ref int width, ref int height);

        [DllImport("CLEyeMulticam.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CLEyeCameraGetFrame(IntPtr camera, IntPtr pData, int waitTimeout);
    }

    public static class CameraDeviceExtensions
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect,
                                                       uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess,
                                                   uint dwFileOffsetHigh, uint dwFileOffsetLow,
                                                   uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr hMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        private static bool setPointersToCamera(CameraDevice dev, IntPtr cameraPointer, IntPtr memorySection,
                                                IntPtr memoryMapping)
        {
            if (cameraPointer == IntPtr.Zero || memorySection == IntPtr.Zero || memoryMapping == IntPtr.Zero)
                return false;
            dev.CameraPointer = cameraPointer;
            dev.MemorySection = memorySection;
            dev.MemoryMapping = memoryMapping;
            return true;
        }


        public static bool CameraGrabFrame(this CameraDevice device, int timeout)
        {
            if (!device.CamaraCreated)
            {
                if (Logger.GetInstance().IsDebugEnabled)
                {
                    Logger.GetInstance()
                          .AddEntry(DebugMode.Debug.Description() + "CameraDevice GrabFrame",
                                    "Camera Device has not been created.");
                }
                return false;
            }
            return CLEyeMulticamAPI.CLEyeCameraGetFrame(device.CameraPointer, device.MemoryMapping, timeout);
        }

        public static bool CameraCreateInstance(this CameraDevice device)
        {
            IntPtr _camPtr = CLEyeMulticamAPI.CLEyeCreateCamera(device.UUID, device.Parameter.ColorMode,
                                                                device.Parameter.Resolution, device.Parameter.FrameRate);
            if (_camPtr == IntPtr.Zero)
            {
                if (Logger.GetInstance().IsDebugEnabled)
                {
                    Logger.GetInstance()
                          .AddEntry(DebugMode.Debug.Description() + "CameraDevice Creation",
                                    "Camera Device can not be created.");
                }

                return false;
            }
            int _w = 0, _h = 0;
            CLEyeMulticamAPI.CLEyeCameraGetFrameDimensions(_camPtr, ref _w, ref _h);
            device.FrameDimension = (device.Parameter.ColorMode ==
                                     CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED ||
                                     device.Parameter.ColorMode == CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_RAW)
                                        ? new CameraDevice.Dimension(_w, _h, 4)
                                        : new CameraDevice.Dimension(_w, _h, 1);
            IntPtr _memSec = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, device.FrameDimension.Size, null);
            IntPtr _memMap = MapViewOfFile(_memSec, 0xF001F, 0, 0, device.FrameDimension.Size);
            if (!setPointersToCamera(device, _camPtr, _memSec, _memMap))
            {
                if (Logger.GetInstance().IsDebugEnabled)
                {
                    Logger.GetInstance()
                          .AddEntry(DebugMode.Debug.Description() + "CameraDevice Creation",
                                    "Memory Footprint can not be created.");
                }
                return false;
            }
            return true;
        }

        public static bool CameraDestroyInstance(this CameraDevice device)
        {
            return CLEyeMulticamAPI.CLEyeDestroyCamera(device.CameraPointer);
        }

        public static bool CameraDestroyFootprint(this CameraDevice device)
        {
            if (device.MemoryMapping != IntPtr.Zero)
            {
                UnmapViewOfFile(device.MemoryMapping);
                device.MemoryMapping = IntPtr.Zero;
            }
            if (device.MemorySection != IntPtr.Zero)
            {
                CloseHandle(device.MemorySection);
                device.MemorySection = IntPtr.Zero;
            }
            return true;
        }

        public static bool CamaraCaptureStart(this CameraDevice device)
        {
            return CLEyeMulticamAPI.CLEyeCameraStart(device.CameraPointer);
        }

        public static bool CameraCaptureStop(this CameraDevice device)
        {
            return CLEyeMulticamAPI.CLEyeCameraStop(device.CameraPointer);
        }

        public static int GetCameraBuiltInControl(this CameraDevice device, CLEyeMulticamAPI.CLEyeCameraParameter param)
        {
            return CLEyeMulticamAPI.CLEyeGetCameraParameter(device.CameraPointer, param);
        }

        public static bool SetCameraBuiltInControl(this CameraDevice device, CLEyeMulticamAPI.CLEyeCameraParameter param,
                                                   int value)
        {
            return CLEyeMulticamAPI.CLEyeSetCameraParameter(device.CameraPointer, param, value);
        }
    }
}
