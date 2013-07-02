using System.ComponentModel;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace Feeder.ViewModel
{
    public class DialogService:IGUIDialogService, INotifyPropertyChanged
    {
        private string _resultMessage;


        public void ShowMessage(object who, string msg)
        {
            raiseMessageNotification((InteractionRequest<INotification>)who,msg);
        }

        public void ShowError(object who,string msg)
        {
            raiseErrorNotification((InteractionRequest<INotification>) who,msg);
        }

        public InteractionRequest<INotification> InitErrorNotificationRequest()
        {
            return new InteractionRequest<INotification>();
        }

        public InteractionRequest<INotification> InitMessageNotificationRequest()
        {
            return new InteractionRequest<INotification>();
        }

        public InteractionRequest<INotification> InitBusyNotificationRequest()
        {
            return new InteractionRequest<INotification>();
        }

        private void raiseErrorNotification(InteractionRequest<INotification> request, string msg)
        {
            // By invoking the Raise method we are raising the Raised event and triggering any InteractionRequestTrigger that
            // is subscribed to it.
            // As parameters we are passing a Notification, which is a default implementation of INotification provided by Prism
            // and a callback that is executed when the interaction finishes.
            request.Raise(
               new Notification { Content = msg, Title = "Error" },
               n => { InteractionResultMessage = "The user was notified about an error."; });
        }
        private void raiseMessageNotification(InteractionRequest<INotification> request, string msg)
        {
            // By invoking the Raise method we are raising the Raised event and triggering any InteractionRequestTrigger that
            // is subscribed to it.
            // As parameters we are passing a Notification, which is a default implementation of INotification provided by Prism
            // and a callback that is executed when the interaction finishes.
            request.Raise(
               new Notification { Content = msg, Title = "Info" },
               n => { InteractionResultMessage = "The user was notified about an info."; });
        }
        
        public string InteractionResultMessage
        {
            get
            {
                return _resultMessage;
            }
            set
            {
                _resultMessage = value;
                OnPropertyChanged("InteractionResultMessage");
            }
        }

        public string FileDialog()
        {
            // Configure save file dialog box
            var _dlg = new Microsoft.Win32.SaveFileDialog
                      {
                          FileName = "Logs",
                          DefaultExt = ".text",
                          Filter = "Text Logs (.txt)|*.txt|(.log)|*.log"
                      };

            // Show save file dialog box
            bool? _result = _dlg.ShowDialog();

            // Process save file dialog box results 
            if (_result == true)
            {
                // Save document 
                string _filename = _dlg.FileName;
                return _filename;
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}