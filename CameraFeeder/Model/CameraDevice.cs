using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Feeder.Configuration;

namespace Feeder.Model
{
    public class CameraDevice : DependencyObject, IDisposable
    {
        public Dimension FrameDimension { get; set; }

        public CameraParameter Parameter { get; set; }

        #region [ Private ]

        //private IntPtr _map = IntPtr.Zero;
        //private IntPtr _section = IntPtr.Zero;
        //private IntPtr _camera = IntPtr.Zero;
        private bool _running;
        private Thread _workerThread;

        #endregion

        #region [ Events ]

        public event EventHandler BitmapReady;

        #endregion

        #region [ Properties ]

        //public float Framerate { get; set; }
        //public CLEyeMulticamAPI.CLEyeCameraColorMode ColorMode { get; set; }
        //public CLEyeMulticamAPI.CLEyeCameraResolution Resolution { get; set; }
        public bool AutoGain
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_GAIN) != 0; }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_GAIN, value ? 1 : 0); }
        }

        public int Gain
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_GAIN); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_GAIN, value); }
        }

        public bool AutoExposure
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE) != 0; }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE, value ? 1 : 0); }
        }

        public int Exposure
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_EXPOSURE); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_EXPOSURE, value); }
        }

        public bool AutoWhiteBalance
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE) != 0; }
            set
            {
                this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE,
                                             value ? 1 : 0);
            }
        }

        public int WhiteBalanceRed
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED, value); }
        }

        public int WhiteBalanceGreen
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN, value); }
        }

        public int WhiteBalanceBlue
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE, value); }
        }

        public bool HorizontalFlip
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_HFLIP) != 0; }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_HFLIP, value ? 1 : 0); }
        }

        public bool VerticalFlip
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_VFLIP) != 0; }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_VFLIP, value ? 1 : 0); }
        }

        public int HorizontalKeystone
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_HKEYSTONE); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_HKEYSTONE, value); }
        }

        public int VerticalKeystone
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_VKEYSTONE); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_VKEYSTONE, value); }
        }

        public int XOffset
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_XOFFSET); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_XOFFSET, value); }
        }

        public int YOffset
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_YOFFSET); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_YOFFSET, value); }
        }

        public int Rotation
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_ROTATION); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_ROTATION, value); }
        }

        public int Zoom
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_ZOOM); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_ZOOM, value); }
        }

        public int LensCorrection1
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION1); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION1, value); }
        }

        public int LensCorrection2
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION2); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION2, value); }
        }

        public int LensCorrection3
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION3); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSCORRECTION3, value); }
        }

        public int LensBrightness
        {
            get { return this.GetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS); }
            set { this.SetCameraBuiltInControl(CLEyeMulticamAPI.CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS, value); }
        }

        #endregion

        #region [ Dependency Properties ]

        private static readonly DependencyPropertyKey _bitmapSourcePropertyKey =
            DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CameraDevice),
                                                new UIPropertyMetadata(default(InteropBitmap)));

        public static readonly DependencyProperty BitmapSourceProperty = _bitmapSourcePropertyKey.DependencyProperty;
        private Bitmap _bitmap;

        public InteropBitmap BitmapSource
        {
            get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
            private set { SetValue(_bitmapSourcePropertyKey, value); }
        }

        public IntPtr CameraPointer { get; set; }

        public IntPtr MemorySection { get; set; }

        public IntPtr MemoryMapping { get; set; }

        public Guid UUID { get; set; }

        public bool CamaraCreated
        {
            get { return CameraPointer != IntPtr.Zero && MemorySection != IntPtr.Zero && MemoryMapping != IntPtr.Zero; }
            set { throw new NotImplementedException(); }
        }

        #endregion

        #region [ Constructor ]

        public CameraDevice()
        {
            Parameter = new CameraParameter(CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED,
                                            CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_QVGA, 50, 10, 2);

        }

        public CameraDevice(Guid uuid)
            : this()
        {
            UUID = uuid;
        }

        public CameraDevice(Guid uuid, object cameraParameter)
            : this(uuid)
        {
            var _p = cameraParameter as CameraParameter;
            if (_p != null)
                Parameter = _p;
        }


        ~CameraDevice()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        #endregion

        #region [ Methods ]

        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                if (BitmapReady != null)
                    BitmapReady(value, null);
            }
        }

        #region [ Interface Implementations ]

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                Stop();
            }
            // free native resources if there are any.
            this.CameraDestroyFootprint();
        }

        #endregion

        public bool Create()
        {
            if (!this.CameraCreateInstance())
                return false;
            BitmapSource =
                Imaging.CreateBitmapSourceFromMemorySection(MemorySection, FrameDimension.Width, FrameDimension.Height,
                                                            FrameDimension.PixelFormat(), FrameDimension.Stride(), 0) as
                InteropBitmap;

            //if (BitmapReady != null) BitmapReady(this, null);
            if (BitmapSource != null)
                BitmapSource.Invalidate();
            return true;
        }


        public void Start()
        {
            _running = true;
            _workerThread = new Thread(captureThread);
            _workerThread.Start();
        }

        public void Stop()
        {
            if (!_running)
                return;
            _running = false;
            _workerThread.Join(1000);
        }

        private void captureThread()
        {
            this.CamaraCaptureStart();

            while (_running)
            {
                if (this.CameraGrabFrame(500))
                {
                    if (!_running)
                        break;
                    Bitmap = new Bitmap(FrameDimension.Width, FrameDimension.Height, FrameDimension.Stride(),
                                        Parameter.ColorFormat, MemoryMapping);
                    Dispatcher.BeginInvoke(DispatcherPriority.Render,
                                           (SendOrPostCallback)delegate { BitmapSource.Invalidate(); }, null);
                }
            }

            this.CameraCaptureStop();
            this.CameraDestroyInstance();
        }

        #endregion

        #region Nested type: Dimension

        public class Dimension
        {
            public Dimension(int w, int h, int d)
            {
                Width = w;
                Height = h;
                Depth = d;
                Size = (uint)Height * (uint)Stride();
            }

            internal uint Size { get; private set; }
            internal int Width { get; private set; }
            internal int Height { get; private set; }
            internal int Depth { get; private set; }

            internal int Stride()
            {
                return Width * Depth;
            }

            internal PixelFormat PixelFormat()
            {
                return Depth == 4 ? PixelFormats.Bgr32 : PixelFormats.Gray8;
            }
        }

        #endregion
    }
}
