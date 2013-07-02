using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace Feeder.ViewModel
{
    public interface IGUIDialogService
    {
        void ShowMessage(object who, string msg);
        void ShowError(object who, string msg);
        InteractionRequest<INotification> InitErrorNotificationRequest();
        InteractionRequest<INotification> InitMessageNotificationRequest();
        InteractionRequest<INotification> InitBusyNotificationRequest();
        string FileDialog();
    }
}
