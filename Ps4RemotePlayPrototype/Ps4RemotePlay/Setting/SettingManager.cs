using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Ps4RemotePlay.Setting
{
    public class SettingManager
    {

        private readonly string _ps4RemotePlayDataSettingsPath;

        private readonly string _ps4RemotePlayDataSettingsFullPath;

        private const string Ps4RemotePlayDataFileName = "PS4RemotePlayData.xml";

        private const string CustomPs4RemotePlayFolderName = "CustomPS4RemotePlay";

        private PS4RemotePlayData _currentRemotePlayData;

        private static SettingManager _instance;

        private SettingManager()
        {
            _ps4RemotePlayDataSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CustomPs4RemotePlayFolderName);
            _ps4RemotePlayDataSettingsFullPath = Path.Combine(_ps4RemotePlayDataSettingsPath, Ps4RemotePlayDataFileName);
            _currentRemotePlayData = ReadRemotePlayData();
        }

        public static SettingManager GetInstance()
        {
            return _instance ?? (_instance = new SettingManager());
        }

        public PS4RemotePlayData GetRemotePlayData()
        {
            return _currentRemotePlayData;
        }

        public bool SavePS4RemotePlayData(PS4RemotePlayData ps4RemotePlayData)
        {
            bool result = true;
            try
            {
                if (!Directory.Exists(_ps4RemotePlayDataSettingsPath))
                {
                    Directory.CreateDirectory(_ps4RemotePlayDataSettingsPath);
                }

                var serializer = new XmlSerializer(ps4RemotePlayData.GetType());
                using (var writer = XmlWriter.Create(_ps4RemotePlayDataSettingsFullPath, new XmlWriterSettings { Indent = true, NewLineHandling = NewLineHandling.Entitize}))
                {
                    serializer.Serialize(writer, ps4RemotePlayData);
                }

                _currentRemotePlayData = ps4RemotePlayData;
                
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        private PS4RemotePlayData ReadRemotePlayData()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PS4RemotePlayData));
                using (var reader = XmlReader.Create(_ps4RemotePlayDataSettingsFullPath))
                {
                    return (PS4RemotePlayData)serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return null;
        }
    }
}
