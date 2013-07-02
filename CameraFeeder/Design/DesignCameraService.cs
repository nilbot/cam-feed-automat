using System;
using System.Collections.ObjectModel;
using Feeder.Model;

namespace Feeder.Design
{
    public class DesignCameraService : ICameraService<CameraDevice>
    {
        #region ICameraService Members

        public void GetAvailableDevices(Action<ObservableCollection<CameraDevice>, Exception> callback)
        {
            var _designPorts = new[] { new Guid(), new Guid(), new Guid(), new Guid(), new Guid(), new Guid(), new Guid() };
            var _designResult = new ObservableCollection<CameraDevice>();
            foreach (var _port in _designPorts)
                _designResult.Add(new CameraDevice(_port));
            callback(_designResult, null);
        }

        #endregion

    }
}
