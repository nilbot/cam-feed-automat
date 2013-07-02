using System;
using System.Collections.ObjectModel;

namespace Feeder.Model
{
    /// <summary>
    ///     Get verfügbaren Interfaces zurück
    /// </summary>
    public interface ICameraService<T>
    {
        /// <summary>
        ///     erstmal suchen ob serials und usb schnittstellen verfügbar sind, wenn ja welche
        ///     dann gibt list von serials zurück, in einem member field
        ///     und usb list zurückgeben ebenfalls, in einem anderen field
        /// </summary>
        /// <param name="callback"></param>
        void GetAvailableDevices(Action<ObservableCollection<T>, Exception> callback);
    }
}
