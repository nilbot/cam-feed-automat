using System;
using System.Collections;
using System.Linq;
using Feeder.Common.Interactions;
using Feeder.Configuration;
using Feeder.Model;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Mvvm;

namespace Feeder.ViewModel
{
    public class ParameterDetailsDialogViewModel : BindableBase, IInteractionRequestAware
    {
        private IEnumerable _defaultValue;
        private ParameterDetailSetupNotification _notification;
        private IEnumerable _allowedValues;

        public ParameterDetailsDialogViewModel()
        {
            ConfirmCommand = new DelegateCommand(AcceptValues);
            CancelCommand = new DelegateCommand(CancelInteraction);
            // This command will be executed when the selection of the ListBox in the view changes.
            SelectedDetailKeyCommand = new DelegateCommand<object[]>(onKeySelected);
            SelectedDetailValueCommand = new DelegateCommand<object[]>(onValueSelected);
        }


        // Both the FinishInteraction and Notification properties will be set by the PopupWindowAction
        // when the popup is shown.

        //public CamInterface SelectedCam { get; set; }

        public DelegateCommand ConfirmCommand { get; private set; }

        public DelegateCommand CancelCommand { get; private set; }

        public object SelectedValue { get; set; }


        public IEnumerable DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                SetProperty(ref _defaultValue, value);
                OnPropertyChanged(() => Notification);
            }
        }

        public DelegateCommand<object[]> SelectedDetailKeyCommand { get; private set; }
        public DelegateCommand<object[]> SelectedDetailValueCommand { get; private set; }
        public string SelectedDetailKey { get; set; }
        public object SelectedDetailValue { get; set; }

        #region IInteractionRequestAware Members

        public Action FinishInteraction { get; set; }

        public INotification Notification
        {
            get { return _notification; }
            set
            {
                if (value is ParameterDetailSetupNotification)
                {
                    // To keep the code simple, this is the only property where we are raising the PropertyChanged event,
                    // as it's required to update the bindings when this property is populated.
                    // Usually you would want to raise this event for other properties too.
                    _notification = value as ParameterDetailSetupNotification;
                    OnPropertyChanged(() => Notification);
                }
            }
        }

        #endregion

        public void AcceptValues()
        {
            if (_notification != null)
            {
                _notification.Confirmed = true;
                OnPropertyChanged(() => Notification);
            }

            FinishInteraction();
        }

        public void CancelInteraction()
        {
            if (_notification != null)
            {
                _notification.Target = new CameraParameter(CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED, CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_QVGA, 50, 10, 1);
                _notification.Confirmed = false;
                OnPropertyChanged(() => Notification);
            }

            FinishInteraction();
        }


        private void onKeySelected(object[] obj)
        {
            if (obj != null && obj.Any())
            {
                var _firstOrDefault = obj.FirstOrDefault();
                if (_firstOrDefault != null)
                {
                    SelectedDetailKey = _firstOrDefault.ToString();
                    AllowedValues = GetAllowedValues(SelectedDetailKey);
                }

            }
        }

        private IEnumerable GetAllowedValues(string selectedDetailKey)
        {
            switch (selectedDetailKey)
            {
                case "FrameRate":
                    return new[] { 15, 30, 50, 60, 100 };

                case "ColorMode":
                    return new[]
                           {
                               CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_MONO_RAW,
                               CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_MONO_PROCESSED,
                               CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_RAW,
                               CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED,
                               CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_BAYER_RAW
                           };
                case "Resolution":
                    return new[]
                           {
                               CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_QVGA,
                               CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_VGA
                           };
                case "PreBufferTimeInSeconds":
                    return new[] { 10, 20, 40 };
                case "PostBufferTimeInSeconds":
                    return new[] { 2, 5, 10 };
            }
            return null;
        }

        public IEnumerable AllowedValues
        {
            get { return _allowedValues; }
            set
            {
                SetProperty(ref _allowedValues, value);
                OnPropertyChanged(() => Notification);
            }
        }

        private void onValueSelected(object[] obj)
        {
            if (obj != null && obj.Any())
            {
                var _firstOrDefault = obj.FirstOrDefault();
                if (_firstOrDefault != null)
                {
                    SelectedDetailValue = _firstOrDefault;
                    setValue(SelectedDetailValue);
                    OnPropertyChanged(() => Notification);
                }
            }
        }

        private void setValue(object selectedDetailValue)
        {
            switch (SelectedDetailKey)
            {
                case "FrameRate":
                    _notification.Target.FrameRate = (int)selectedDetailValue;
                    break;
                case "ColorMode":
                    _notification.Target.ColorMode = (CLEyeMulticamAPI.CLEyeCameraColorMode)selectedDetailValue;
                    break;
                case "Resolution":
                    _notification.Target.Resolution = (CLEyeMulticamAPI.CLEyeCameraResolution)selectedDetailValue;
                    break;
                case "PreBufferTimeInSeconds":
                    _notification.Target.PreBufferTimeInSeconds = (int)selectedDetailValue;
                    break;
                case "PostBufferTimeInSeconds":
                    _notification.Target.PostBufferTimeInSeconds = (int)selectedDetailValue;
                    break;
            }
        }
    }
}
