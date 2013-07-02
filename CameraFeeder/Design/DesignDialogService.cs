using System.Diagnostics;
using Feeder.ViewModel;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace Feeder.Design
{
    public class DesignDialogService : IGUIDialogService
    {
        #region IGUIDialogService Members

        public void ShowMessage(object who, string msg)
        {
            Debug.WriteLine(msg);
        }

        public void ShowError(object who, string msg)
        {
            Debug.WriteLine(msg);
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
        public string FileDialog()
        {
            // Configure save file dialog box
            var _dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Document",
                DefaultExt = ".text",
                Filter = "Text documents (.txt)|*.txt"
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
        #endregion
    }
}
