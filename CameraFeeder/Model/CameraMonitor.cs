using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using AviFile;
using Feeder.Configuration;
using System.Drawing.Imaging;
using Feeder.Properties;

namespace Feeder.Model
{
    public delegate StateFn StateFn();
    public class CameraMonitor : /*Image,*/IDisposable
    {
        private bool _cueRecord;

        public bool CueRecord
        {
            get
            {
                return _cueRecord;
            }
            set
            {
                CueRun = false;
                CueIdle = false;
                CueAbort = false;
                _cueRecord = value;
            }
        }
        private bool _cueRun;

        public bool CueRun
        {
            get
            {
                return _cueRun;
            }
            set
            {
                CueRecord = false;
                CueIdle = false;
                CueAbort = false;
                _cueRun = value;
            }
        }
        private bool _cueIdle;

        public bool CueIdle
        {
            get
            {
                return _cueIdle;
            }
            set
            {
                CueRun = false;
                CueRecord = false;
                CueAbort = false;
                _cueIdle = value;
            }
        }
        private bool _cueAbort;
        public bool CueAbort
        {
            get
            {
                return _cueAbort;
            }
            set
            {
                CueRun = false;
                CueIdle = false;
                CueRecord = false;
                _cueAbort = value;
            }
        }


        private readonly IMasterStateMachine _controller;
        private readonly Stack<StateMachineEnum> _stateStorage;
        private Bitmap[] _data;
        private EventHandler _savingIsDone;
        private Thread _workingThread;
        private CancellationTokenSource _workingThreadCancellation;

        public CameraMonitor(Guid device, CameraParameter cameraParameter, IMasterStateMachine masterController)
        {
            Device = new CameraDevice(device, cameraParameter);
            RingBuffer = new RingBuffer(cameraParameter.FrameRate * cameraParameter.PreBufferTimeInSeconds, cameraParameter.FrameRate * cameraParameter.PostBufferTimeInSeconds);
            RingBuffer.SnapshotReady += SnapshotReady;
            Device.BitmapReady += onBitmapReady;
            _controller = masterController;
            _stateStorage = new Stack<StateMachineEnum>();

            _controller.UniversalStart += start;
            _controller.UniversalStop += stop;
            CueIdle = true;
            new Thread(runSm).Start();
        }

        private void runSm()
        {
            StateFn _state = initFn;
            while (_state != null)
            {
                _state = _state();
            }
        }

        public StateMachineEnum CurrentState { get; set; }

        public CameraDevice Device { get; private set; }

        public RingBuffer RingBuffer { get; private set; }

        public string CameraName { get { return _controller.NameDictionary[Device.UUID]; } }

        public int Width
        {
            get { return Device.FrameDimension.Width; }
        }

        public int Height
        {
            get { return Device.FrameDimension.Height; }
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _controller.UniversalStart -= start;
            _controller.UniversalStop -= stop;
            _savingIsDone = null;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void updateState(StateMachineEnum next)
        {
            _stateStorage.Push(CurrentState);
            CurrentState = next;
        }

        private StateFn initFn()
        {
            _savingIsDone += handleSavingIsDone;
            RingBuffer.SetPreRingSize(Device.Parameter.FrameRate * Device.Parameter.PreBufferTimeInSeconds);
            RingBuffer.SetPostRingSize(Device.Parameter.FrameRate * Device.Parameter.PostBufferTimeInSeconds);
            return idleLoop;
        }

        private StateFn idleLoop()
        {
            if (CueRun)
            {
                Device.AutoExposure = true;
                Device.AutoGain = true;
                Device.Start();
                return runLoop;
            }
            if (CueIdle)
            {
                return idleLoop;
            }
            return null;

        }

        private StateFn runLoop()
        {
            if (CueIdle)
            {
                Device.Stop();
                Dispose();
                return idleLoop;
            }
            if (CueRun)
            {
                return runLoop;
            }
            if (CueRecord)
            {
                RingBuffer.SnapshotReady -= SnapshotReady;
                RingBuffer.SnapshotReady += SnapshotReady;
                var _test = new Bitmap(Resources.smiley_face_with_button_ctrl);
                RingBuffer.SavePreSnapshot(_test);
                var _tuple = this.CreateVideoStream();
                RegisterSaveFileTask(_tuple);
                return savingState;
            }
            return null;
        }

        private StateFn savingState()
        {
            if (CueRun)
            {
                clearWorkStatus();
                return runLoop;
            }
            if (CueAbort)
            {
                abortWork();
                return null;
            }
            return savingState;
        }
        public void Signaled(object sender, EventArgs e)
        {

            //foreach (var _a in getCameraStateMachineAdjacencyList(CurrentState))
            //{
            //    if (_a.Next == StateMachineEnum.SAVING)
            //    {
            //        updateState(StateMachineEnum.SAVING);
            //        _a.Execute();
            //    }
            //}
            CueRecord = true;

        }

        private void stop(object sender, EventArgs e)
        {
            //foreach (var _a in getCameraStateMachineAdjacencyList(CurrentState))
            //{
            //    if (_a.Next == StateMachineEnum.STOPPED)
            //    {
            //        updateState(StateMachineEnum.STOPPED);
            //        _a.Execute();
            //    }
            //}
            CueIdle = true;
        }

        private void start(object sender, EventArgs e)
        {
            //foreach (var _a in getCameraStateMachineAdjacencyList(CurrentState))
            //{
            //    if (_a.Next == StateMachineEnum.RUNNING)
            //    {
            //        updateState(StateMachineEnum.RUNNING);
            //        _a.Execute();
            //    }
            //}
            CueRun = true;
        }

        ~CameraMonitor()
        {
            Dispose(false);
        }

        public void Dispose(bool b)
        {
            if (b)
            {
                if (Device != null)
                {
                    Device.Stop();
                    Device.Dispose();
                    Device = null;
                }
                if (RingBuffer.Count != 0)
                {
                    RingBuffer.SnapshotReady -= SnapshotReady;
                    RingBuffer.Clear();
                    RingBuffer = null;
                }
            }
        }

        private void onBitmapReady(object src, EventArgs ea)
        {
            RingBuffer.Enqueue(Device.Bitmap);
        }

/*
        private IEnumerable<StateMachineTraverse> getCameraStateMachineAdjacencyList(StateMachineEnum oldState)
        {
            var _result = new List<StateMachineTraverse>();
            switch (oldState)
            {
                case StateMachineEnum.SAVING:
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.RUNNING, clearWorkStatus));
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.STOPPED, abortWork));
                    break;
                case StateMachineEnum.STOPPED:
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.RUNNING, () =>
                    {
                        _savingIsDone += handleSavingIsDone;
                        RingBuffer.SetPreRingSize(Device.Parameter.FrameRate * Device.Parameter.PreBufferTimeInSeconds);
                        RingBuffer.SetPostRingSize(Device.Parameter.FrameRate * Device.Parameter.PostBufferTimeInSeconds);
                        Device.AutoExposure = true;
                        Device.AutoGain = true;
                        Device.Start();
                    }));
                    break;
                case StateMachineEnum.RUNNING:
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.STOPPED, Dispose));
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.SAVING, () =>
                    {
                        RingBuffer.SnapshotReady -= SnapshotReady;
                        RingBuffer.SnapshotReady += SnapshotReady;
                        var _test = new Bitmap(Resources.smiley_face_with_button_ctrl);
                        RingBuffer.SavePreSnapshot(_test);
                        var _tuple = this.CreateVideoStream();
                        RegisterSaveFileTask(_tuple);
                    }));
                    break;
            }


            return _result;
        }
*/

        private void handleSavingIsDone(object sender, EventArgs e)
        {
            if (!(bool)sender)
            {
                RingBuffer.Clear();
                updateState(StateMachineEnum.RUNNING);
                CueRun = true;
            }
            else
            {
                updateState(StateMachineEnum.STOPPED);
                Dispose();
                CueAbort = true;
            }
        }




        public void RegisterSaveFileTask(AviManager manager)
        {


            _workingThread = new Thread(threadStart =>
            {

                Bitmap _tmp32Bit;
                Bitmap _tmp8Bit;

                ColorPalette _grayPalette = ((ThreadStartParameter)threadStart).Data.First().Palette;
                if (Device.Parameter.ColorFormat == PixelFormat.Format32bppRgb)
                {
                    _tmp32Bit = ((ThreadStartParameter)threadStart).Data.First().Clone(new Rectangle(0, 0, ((ThreadStartParameter)threadStart).Data.First().Width, ((ThreadStartParameter)threadStart).Data.First().Height), Device.Parameter.ColorFormat);
                }
                else
                {
                    _tmp8Bit = ((ThreadStartParameter)threadStart).Data.First().Clone(new Rectangle(0, 0, ((ThreadStartParameter)threadStart).Data.First().Width, ((ThreadStartParameter)threadStart).Data.First().Height), Device.Parameter.ColorFormat);
                    _grayPalette = _tmp8Bit.Palette;
                    // setup gray-scale palette
                    for (int _j = 0; _j < _grayPalette.Entries.Length; _j++)
                        _grayPalette.Entries[_j] = Color.FromArgb(_j, _j, _j);
                    _tmp8Bit.Palette = _grayPalette;
                    _tmp32Bit = new Bitmap(_tmp8Bit);
                }
                var _vs = manager.AddVideoStream(false, 10, _tmp32Bit);

                foreach (var _bitmap in ((ThreadStartParameter)threadStart).Data)
                //for (int n = 0; n<tmp.Length; n++)
                {
                    if (((ThreadStartParameter)threadStart).Token.IsCancellationRequested)
                    {
                        manager.Close();
                        dispose(ref _data); 
                        return;
                    }


                    if (Device.Parameter.ColorFormat == PixelFormat.Format32bppRgb)
                    {
                        _tmp32Bit = _bitmap.Clone(new Rectangle(0, 0, ((ThreadStartParameter)threadStart).Data.First().Width, ((ThreadStartParameter)threadStart).Data.First().Height), Device.Parameter.ColorFormat);
                    }
                    else
                    {
                        _tmp8Bit = _bitmap.Clone(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), Device.Parameter.ColorFormat);
                        _tmp8Bit.Palette = _grayPalette;
                        _tmp32Bit = new Bitmap(_tmp8Bit);
                    }

                    _vs.AddFrame(_tmp32Bit);
                    _tmp32Bit.Dispose();
                    

                }

                manager.Close();
                dispose(ref _data);

            });
        }

        private void dispose(ref Bitmap[] data)
        {
            foreach (var _d in data)
            {
                _d.Dispose();
            }
            data = null;
        }

        public void SnapshotReady(object sender, EventArgs eventArgs)
        {
            Task.Run(() =>
            {
                 _data = RingBuffer.GrabSnapshot();
                _workingThreadCancellation = new CancellationTokenSource();
                _workingThread.Start(new ThreadStartParameter(_workingThreadCancellation.Token, _data));
                _workingThread.Join();
                
                if (_savingIsDone != null)
                    _savingIsDone(_workingThreadCancellation.Token.IsCancellationRequested, null);
            });
        }
        private class ThreadStartParameter
        {
            public CancellationToken Token;
            public readonly IEnumerable<Bitmap> Data;

            public ThreadStartParameter(CancellationToken token, IEnumerable<Bitmap> data)
            {
                Token = token;
                Data = data;
            }
        }

        private void abortWork()
        {
            if (_workingThread != null && _workingThread.ThreadState == ThreadState.Unstarted)
            {
                RingBuffer.SnapshotReady -= SnapshotReady;
                clearWorkStatus();
                Dispose();
            }
            else if (_workingThread != null && _workingThread.ThreadState == ThreadState.Running)
                _workingThreadCancellation.Cancel();
        }

        private void clearWorkStatus()
        {
            _workingThread = null;
            _data = null;
            //Dispose();
        }
    }
}
