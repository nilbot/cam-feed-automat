using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Feeder.Common.Interactions;
using Feeder.Configuration;
using Feeder.Model;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Mvvm;

namespace Feeder.ViewModel
{
    /// <summary>
    ///     base viewmodel contains all shared gui related common behaviors
    /// </summary>
    public class CommonViewModelBase : BindableBase
    {
        #region Delegates

        public delegate void DelegateParameterChanged(CameraParameter param);

        public delegate void DelegateStateChanged(StateMachineEnum oldState, StateMachineEnum newState);

        #endregion

        private readonly ICameraService<Guid> _cameraService;
        private readonly IGUIDialogService _dialogService;
        private ObservableCollection<Guid> _camaraDevices;
        private DelegateCommand _getCameraGuids;
        private CameraParameter _parameters;


        /// <summary>
        ///     default ctor that takes implementations
        /// </summary>
        /// <param name="cameraService">camera service, which provides interfaces and manage their AL</param>
        /// <param name="dialogService">gui service</param>
        public CommonViewModelBase(ICameraService<Guid> cameraService, IGUIDialogService dialogService)
        {
            _cameraService = cameraService;
            _dialogService = dialogService;

            ErrorNotificationRequest = _dialogService.InitErrorNotificationRequest();
            MessageNotificationRequest = _dialogService.InitMessageNotificationRequest();
            BusyNotificationRequest = _dialogService.InitBusyNotificationRequest();
            ParameterDetailSetupNotificationRequest = new InteractionRequest<ParameterDetailSetupNotification>();

            _cameraService.GetAvailableDevices((interfaces, exception) =>
            {
                if (exception != null)
                {
                    _dialogService.ShowError(ErrorNotificationRequest,
                                             string.Format(
                                                 "Be adviced, there are no connected cameras. Please Plug-in and refresh devices.\nCameraService returned error message:{0}",
                                                 exception));
                    return;
                }

                CameraDevices = interfaces;
            });
        }

        public ICommand GetCameraUuids
        {
            get
            {
                return (ICommand) _getCameraGuids ??
                       (ICommand) (_getCameraGuids = new DelegateCommand(populateCameraDeviceIds));
            }
        }

        public CameraParameter Parameters
        {
            get { return _parameters; }
            set { SetProperty(ref _parameters, value); }
        }

        public IGUIDialogService DialogService
        {
            get { return _dialogService; }
        }


        public InteractionRequest<ParameterDetailSetupNotification> ParameterDetailSetupNotificationRequest { get; private set; }

        public InteractionRequest<INotification> BusyNotificationRequest { get; private set; }

        public InteractionRequest<INotification> MessageNotificationRequest { get; private set; }


        public InteractionRequest<INotification> ErrorNotificationRequest { get; private set; }

        /// <summary>
        ///     Sets and gets the CamaraDevices property.
        ///     Changes to that property's value raise the PropertyChanged event.
        /// </summary>
        public ObservableCollection<Guid> CameraDevices
        {
            get { return _camaraDevices; }
            set { SetProperty(ref _camaraDevices, value); }
        }

        private void populateCameraDeviceIds()
        {
            _cameraService.GetAvailableDevices((interfaces, exception) =>
            {
                if (exception != null)
                {
                    _dialogService.ShowError(ErrorNotificationRequest,
                                             string.Format("CameraService returned error message:{0}", exception));
                    return;
                }
                if (CameraDevices.Count != 0)
                    CameraDevices.Clear();
                CameraDevices = interfaces;
            });
        }

        //public IEnumerable<ValueDescription> CamParamList
        //{
        //    get { return EnumHelper.GetAllValuesAndDescriptions<CamParamEnum>(); }
        //}


        //private void onComInterfaceSelected(object[] items)
        //{
        //    if (SelectedCam != null && SelectedCam.Connection.IsOpen)
        //        closeConnection();

        //    if (items != null && items.Any())
        //    {
        //        if ((SelectedCam = (CamInterface)items.FirstOrDefault()) != null)
        //        {
        //            SelectedComName = SelectedCam.Name;
        //            //raiseSetConnectionDetailAction();
        //            ConnectionButtonClickable = true;
        //            closeConnection();
        //        }
        //    }


        //}
    }
}
