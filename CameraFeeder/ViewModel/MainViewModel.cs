using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Feeder.Common.Interactions;
using Feeder.Common.Timber;
using Feeder.Configuration;
using Feeder.Model;
using Microsoft.Practices.Prism.Commands;
using ServiceStack.Text;
//using TwinCAT.Ads;

namespace Feeder.ViewModel
{
    /// <summary>
    ///     Main
    /// </summary>
    public class MainViewModel : CommonViewModelBase, IMasterStateMachine
    {
        private DelegateCommand _cameraControl;
        private string _cameraMissionControlButtonBackground;
        private DelegateCommand _clearAllLogEntries;
        private string _daText;
        private ICommand _fireRecord;
        private ICommand _fireRun;
        private ICommand _fireStop;

        private bool _loggerIncludeInfo;
        private DelegateCommand _raiseSetParameterDetailAction;
        private DelegateCommand _saveAllLogEntries;
        private StateMachineEnum _state;

        private Stack<StateMachineEnum> _stateStorage;

        /// <summary>
        ///     Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(ICameraService<Guid> cameraService, IGUIDialogService dialogService)
            : base(cameraService, dialogService)
        {
            //http://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
            LogEntries = Logger.GetInstance().GetLogger();

            LoggerIncludeInfo = true;


            RaiseParameterChangedEvent += handleParameterChangedEvent;
            RaiseStateChangingEvent += handleStateChangingEvent;

            this.LoadOrCreateStorage();

            initWorkFlow();
        }


        public ObservableCollection<CameraMonitor> CameraMonitors { get; set; }


        public ObservableCollection<LogEntry> LogEntries { get; private set; }


        public ICommand ClearAllLogEntries
        {
            get
            {
                return (ICommand)_clearAllLogEntries ??
                       (ICommand)(_clearAllLogEntries = new DelegateCommand(() => LogEntries.Clear()));
            }
        }

        public ICommand SaveAllLogEntries
        {
            get
            {
                return (ICommand)_saveAllLogEntries ??
                       (ICommand)(_saveAllLogEntries = new DelegateCommand(executeSaveAllLogs));
            }
        }

        /// <summary>
        ///     Gets the RollTheCameras.
        /// </summary>
        public ICommand RollTheCameras
        {
            get
            {
                return (ICommand)_cameraControl ??
                       (ICommand)(_cameraControl = new DelegateCommand(executeRollTheCameras));
            }
        }

        protected string CameraMissionControlButtonText
        {
            get { return _daText; }
            set { SetProperty(ref _daText, value); }
        }

        public ICommand FireRun
        {
            get { return _fireRun ?? (_fireRun = (ICommand)new DelegateCommand(executeFireRun)); }
        }

        public ICommand FireStop
        {
            get { return _fireStop ?? (_fireStop = (ICommand)new DelegateCommand(executeFireStop)); }
        }

        public ICommand FireRecord
        {
            get { return _fireRecord ?? (_fireRecord = (ICommand)new DelegateCommand(executeFireRecord)); }
        }

        protected StateMachineEnum CurrentState
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
        }


        public ICommand RaiseSetParameterDetailAction
        {
            get
            {
                return (ICommand)_raiseSetParameterDetailAction ??
                       (ICommand)(_raiseSetParameterDetailAction = new DelegateCommand(raiseSetParameterDetailAction));
            }
        }

        public bool LoggerIncludeInfo
        {
            get { return _loggerIncludeInfo; }
            set { SetProperty(ref _loggerIncludeInfo, value); }
        }

        public string CameraMissionControlButtonBackground
        {
            get { return _cameraMissionControlButtonBackground; }
            set { SetProperty(ref _cameraMissionControlButtonBackground, value); }
        }

        //protected List<TcAdsClient> BeckhoffClients { get; set; }

        internal CameraDictionary DeviceLookupTable { get; set; }

        internal BeckhoffConfig BeckHoffConfiguration { get; set; }

        #region IMasterStateMachine Members

        public event EventHandler UniversalStart;
        public event EventHandler UniversalStop;

        #endregion

        private void initWorkFlow()
        {
            CameraMonitors = new ObservableCollection<CameraMonitor>();


            //build state machine
            _stateStorage = new Stack<StateMachineEnum>();
            _stateStorage.Clear();


            //renderInit
            CameraMissionControlButtonBackground = "Grey";
            CameraMissionControlButtonText = "Ready, Press To Capture";

            CurrentState = StateMachineEnum.STOPPED;
        }
        public Dictionary<Guid, string> NameDictionary { get { return DeviceLookupTable.CameraNameTable; } }

        private void createMonitors()
        {
            if (CameraMonitors.Count != 0)
                CameraMonitors.Clear();
            foreach (var _cameraDevice in CameraDevices)
            {
                var _ci = new CameraMonitor(_cameraDevice, Parameters, this);
                if (!_ci.Device.Create())
                    DebugMode.Error.Log("Camera Instance Creation Failed.");
                CameraMonitors.Add(_ci);
            }
        }

        //private ICommand _connectBeckhoff;
        //public ICommand ConnectBeckhoff { get { return _connectBeckhoff ?? (_connectBeckhoff = (ICommand)new DelegateCommand(connectBeckhoff)); } }
        //private void connectBeckhoff()
        //{
        //    BeckhoffClients = new List<TcAdsClient>();
        //    for (int _c = 0; _c != BeckHoffConfiguration.BeckhoffVariables.Length; _c++)
        //    {
        //        try
        //        {
        //            var _cli = new TcAdsClient { Synchronize = false };
        //            _cli.Connect(BeckHoffConfiguration.BeckhoffConnectionId,
        //                         BeckHoffConfiguration.BeckhoffConnectionPort);
        //            _cli.AddDeviceNotification(BeckHoffConfiguration.Index(_c), new AdsStream(2), AdsTransMode.OnChange,
        //                                       100, 0, null);
        //            _cli.AdsNotification +=
        //                CameraMonitors.Single(
        //                    monitor =>
        //                    monitor.Device.UUID ==
        //                    DeviceLookupTable.BeckhoffVariableTable[BeckHoffConfiguration.Index(_c)]).Signaled;
        //            BeckhoffClients.Add(_cli);
        //        }
        //        catch (Exception _e)
        //        {
        //            UiInvoke(() => Logger.GetInstance().AddEntry("Beckhoff", _e.Message));
        //        }
        //    }
        //}


        private void updateState(StateMachineEnum next)
        {
            _stateStorage.Push(CurrentState);
            CurrentState = next;
        }

        private void handleParameterChangedEvent(CameraParameter param)
        {
            Parameters = param;
            var _last = _stateStorage.Peek();
            if (_last != StateMachineEnum.SAVING)
                RaiseStateChangingEvent(_last, StateMachineEnum.STOPPED);
        }


        public event DelegateParameterChanged RaiseParameterChangedEvent;

        public event DelegateStateChanged RaiseStateChangingEvent;


        private void handleStateChangingEvent(StateMachineEnum oldState, StateMachineEnum newState)
        {
            var _actions = getMasterStateMachineAdjacencyList(oldState);
            foreach (StateMachineTraverse _action in _actions)
            {
                if (_action.Next == newState)
                {
                    updateState(newState);
                    _action.Execute();
                }
            }
        }

        private IEnumerable<StateMachineTraverse> getMasterStateMachineAdjacencyList(StateMachineEnum oldState)
        {
            var _result = new List<StateMachineTraverse>();
            switch (oldState)
            {
                case StateMachineEnum.STOPPED:
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.RUNNING, () =>
                    {
                        UiInvoke(() =>
                        {
                            CameraMissionControlButtonBackground = "Green";
                            CameraMissionControlButtonText = "Capturing, Press To Stop";
                        });

                        createMonitors();
                        UniversalStart(this, null);
                    }));
                    break;
                case StateMachineEnum.RUNNING:
                    _result.Add(new StateMachineTraverse(oldState, StateMachineEnum.STOPPED, () =>
                    {
                        UniversalStop(this, null);
                        UiInvoke(() =>
                        {
                            CameraMonitors.Clear();
                            CameraMissionControlButtonBackground = "Grey";
                            CameraMissionControlButtonText = "Capture Stopped, Press to Roll Again.";
                        });
                    }));

                    break;
            }


            return _result;
        }

        private void executeSaveAllLogs()
        {
            var _ret = DialogService.FileDialog();
            using (var _ms = new MemoryStream())
            {
                foreach (var _logEntry in LogEntries)
                    JsonSerializer.SerializeToStream(_logEntry, _ms);
                _ms.WriteTo(File.OpenWrite(_ret));
            }
        }

        private void executeRollTheCameras()
        {
            switch (CurrentState)
            {
                case StateMachineEnum.RUNNING:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.STOPPED);
                    break;
                case StateMachineEnum.STOPPED:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.RUNNING);
                    break;
            }
        }


        private void executeFireRun()
        {
            switch (CurrentState)
            {
                case StateMachineEnum.STOPPED:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.RUNNING);
                    break;
                case StateMachineEnum.SAVING:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.RUNNING);
                    break;
            }
        }

        private void executeFireStop()
        {
            switch (CurrentState)
            {
                case StateMachineEnum.RUNNING:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.STOPPED);
                    break;
                case StateMachineEnum.SAVING:
                    RaiseStateChangingEvent(CurrentState, StateMachineEnum.STOPPED);
                    break;
            }
        }

        private void executeFireRecord()
        {
            //switch (CurrentState)
            //{
            //    case StateMachineEnum.RUNNING:
            //        RaiseStateChangingEvent(CurrentState, StateMachineEnum.SAVING);
            //        break;
            //    case StateMachineEnum.STOPPED:
            //        RaiseStateChangingEvent(CurrentState, StateMachineEnum.SAVING);
            //        break;
            //}
            foreach (var _monitor in CameraMonitors)
                _monitor.Signaled(this, null);
        }

        private void raiseSetParameterDetailAction()
        {
            var _notification = new ParameterDetailSetupNotification
            {
                Title =
                    string.Format(
                        "Please specify the details of parameters")
            };

            ParameterDetailSetupNotificationRequest.Raise(_notification, returned =>
            {
                if (returned != null)
                {
                    DialogService.ShowMessage(MessageNotificationRequest, "Parameter Manually Set");


                    RaiseParameterChangedEvent(returned.Target);
                }
            });
        }

        public static void UiInvoke(Action a)
        {
            Application.Current.Dispatcher.BeginInvoke(a);
        }
    }
}
