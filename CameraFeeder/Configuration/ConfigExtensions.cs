using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Feeder.Model;
using Feeder.Properties;
using Feeder.ViewModel;
using ServiceStack.Text;

namespace Feeder.Configuration
{
    public static class ConfigExtensions
    {
        public static string Index(this BeckhoffConfig config, int i)
        {
            return config.BeckhoffVariables[i];
        }

        public static void LoadOrCreateStorage(this MainViewModel mainViewModel)
        {
            var _storagePath = Environment.ExpandEnvironmentVariables(Resources.UserPath);
            var _configPath = Environment.ExpandEnvironmentVariables(Resources.UserConfigPath);
            var _dataPath = Environment.ExpandEnvironmentVariables(Resources.UserDataPath);

            var _parameterConfig = _configPath + @"\parameter.json";
            var _beckhoffConfig = _configPath + @"\beckhoff.json";
            var _cameraNameConfig = _configPath + @"\names.json";

            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);
            if (!Directory.Exists(_configPath))
                Directory.CreateDirectory(_configPath);

            // write default settings

            #region Default Settings

            var _defaultCameraParameter =
                new CameraParameter(CLEyeMulticamAPI.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED,
                                    CLEyeMulticamAPI.CLEyeCameraResolution.CLEYE_QVGA, 50, 10, 1);
            var _defaultBeckhoffSetting = new BeckhoffConfig
            {
                BeckhoffConnectionId = "Example.Id",
                BeckhoffConnectionPort = 8081,
                BeckhoffVariables =
                    new[]
                                                  {
                                                      ".SB0000_A_Output.Example.Foo.Bar.RunFlow.mErrorId"
                                                      ,
                                                      ".SB0000_B_Output.Example.Foo.Bar.RunFlow.mErrorId"
                                                      ,
                                                      ".SB0000_C_Output.Example.Foo.Bar.RunFlow.mErrorId"
                                                      ,
                                                      ".SB0000_D_Output.Example.Foo.Bar.RunFlow.mErrorId"
                                                  }
            };

            var _initialCameraListing = new CameraDictionary();
            var _cn = new Dictionary<Guid, string>();
            var _bv = new Dictionary<string, Guid>();
            int _int = 0;
            foreach (var _cameraDevice in mainViewModel.CameraDevices)
            {
                _cn[_cameraDevice] = String.Format("Camera {0}", Interlocked.Increment(ref _int));
            }
            for (int _i = 0; _i != mainViewModel.CameraDevices.Count; _i++)
            {
                _bv[_defaultBeckhoffSetting.Index(_i)] = mainViewModel.CameraDevices[_i];
            }
            _initialCameraListing.CameraNameTable = _cn;
            _initialCameraListing.BeckhoffVariableTable = _bv;

            #endregion


            if (!File.Exists(_parameterConfig))
            {
                using (var _ms = new MemoryStream())
                {
                    JsonSerializer.SerializeToStream(_defaultCameraParameter, _ms);
                    using (var _fs = File.OpenWrite(_parameterConfig))
                        _ms.WriteTo(_fs);
                }
            }
            if (!File.Exists(_beckhoffConfig))
            {
                using (var _ms = new MemoryStream())
                {
                    JsonSerializer.SerializeToStream(_defaultBeckhoffSetting, _ms);
                    using (var _fs = File.OpenWrite(_beckhoffConfig))
                        _ms.WriteTo(_fs);
                }
            }
            if (!File.Exists(_cameraNameConfig))
            {
                using (var _ms = new MemoryStream())
                {
                    JsonSerializer.SerializeToStream(_initialCameraListing, _ms);
                    using (var _fs = File.OpenWrite(_cameraNameConfig))
                        _ms.WriteTo(_fs);
                }
            }

            #region test

            // test


            // test done

            #endregion

            #region load settings and parameters

            using (var _fs = File.OpenRead(_parameterConfig))
            {
                mainViewModel.Parameters = JsonSerializer.DeserializeFromStream<CameraParameter>(_fs);
            }

            using (var _fs = File.OpenRead(_beckhoffConfig))
            {
                mainViewModel.BeckHoffConfiguration = JsonSerializer.DeserializeFromStream<BeckhoffConfig>(_fs);
            }

            using (var _fs = File.OpenRead(_cameraNameConfig))
            {
                mainViewModel.DeviceLookupTable = JsonSerializer.DeserializeFromStream<CameraDictionary>(_fs);
            }

            #endregion
        }
    }
}
