using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ps4RemotePlay.Protocol.Message
{
    public class StreamResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class StreamParameters
    {
        public StreamResolution Resolution { get; set; }
        public int MaxFps { get; set; }
        public int Score { get; set; }
    }

    public class Network
    {
        public int BwKbpsSent { get; set; }
        public Decimal BwLoss { get; set; }
        public int Mtu { get; set; }
        public int Rtt { get; set; }
        public List<int> Ports { get; set; }
    }

    public class AppSpecification
    {
        public int MinFps { get; set; }
        public int MinBandwidth { get; set; }
        public string ExtTitleId { get; set; }
        public int Version { get; set; }
        public int TimeLimit { get; set; }
        public int StartTimeout { get; set; }
        public int AfkTimeout { get; set; }
        public int AfkTimeoutDisconnect { get; set; }
    }

    public class Konan
    {
        public string Ps3AccessToken { get; set; }
        public string Ps3RefreshToken { get; set; }
    }

    public class GameSpecification
    {
        public string Model { get; set; }
        public string Platform { get; set; }
        public string AudioChannels { get; set; }
        public string Language { get; set; }
        public string AcceptButton { get; set; }
        public List<string> ConnectedControllers { get; set; }
        public string YuvCoefficient { get; set; }
        public string VideoEncoderProfile { get; set; }
        public string AudioEncoderProfile { get; set; }
    }

    public class UserProfile
    {
        public string OnlineId { get; set; }
        public string NpId { get; set; }
        public string Region { get; set; }
        public List<string> LanguagesUsed { get; set; }
    }

    public class LaunchSpecification
    {
        public string SessionId { get; set; }
        public List<StreamParameters> StreamResolutions { get; set; }
        public Network Network { get; set; }
        public int SlotId { get; set; }
        public AppSpecification AppSpecification { get; set; }
        public Konan Konan { get; set; }
        public GameSpecification RequestGameSpecification { get; set; }
        public UserProfile UserProfile { get; set; }
        public byte[] HandshakeKey { get; set; }

        public static JsonSerializerSettings SerializerSettings
            => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, SerializerSettings) + "\u0000";
        }

        public static LaunchSpecification Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<LaunchSpecification>(json, SerializerSettings);
        }

        public static LaunchSpecification GetStandardSpecs(string sessionId, byte[] handshakeKey)
        {
            return new LaunchSpecification()
            {
                SessionId = sessionId,
                StreamResolutions = new List<StreamParameters>()
                {
                    new StreamParameters()
                    {
                        Resolution = new StreamResolution()
                        {
                            Width = 1280,
                            Height = 720
                        },
                        MaxFps = 60,
                        Score = 10
                    }
                },
                Network = new Network()
                {
                    BwKbpsSent = 10000,
                    BwLoss = 0.001000M,
                    Mtu = 1454,
                    Rtt = 5,
                    Ports = new List<int>()
                    {
                        53,
                        2053
                    }
                },
                SlotId = 1,
                AppSpecification = new AppSpecification()
                {
                    MinFps = 60,
                    MinBandwidth = 0,
                    ExtTitleId = "ps3",
                    Version = 1,
                    TimeLimit = 1,
                    StartTimeout = 100,
                    AfkTimeout = 100,
                    AfkTimeoutDisconnect = 100
                },
                Konan = new Konan()
                {
                    Ps3AccessToken = "accessToken",
                    Ps3RefreshToken = "refreshToken"
                },
                RequestGameSpecification = new GameSpecification()
                {
                    Model = "bravia_tv",
                    Platform = "android",
                    AudioChannels = "5.1",
                    Language = "sp",
                    AcceptButton = "X",
                    ConnectedControllers = new List<string>()
                    {
                        "xinput",
                        "ds3",
                        "ds4"
                    },
                    YuvCoefficient = "bt601",
                    VideoEncoderProfile = "hw4.1",
                    AudioEncoderProfile = "audio1"
                },
                UserProfile = new UserProfile()
                {
                    OnlineId = "psnId",
                    NpId = "npId",
                    Region = "US",
                    LanguagesUsed = new List<string>()
                    {
                        "en",
                        "jp"
                    }
                },
                HandshakeKey = handshakeKey
            };
        }
    }
}
