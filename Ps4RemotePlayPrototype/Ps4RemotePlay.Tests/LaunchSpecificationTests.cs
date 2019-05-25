using System;
using System.Text;
using Xunit;

using Ps4RemotePlay.Protocol.Message;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Tests
{
    public class LaunchSpecificationTests
    {
        readonly string expectedLaunchSpec = "{\"sessionId\":\"sessionId4321\",\"streamResolutions\":[{\"resolution\":{\"width\":1280,\"height\":720},\"maxFps\":60,\"score\":10}],\"network\":{\"bwKbpsSent\":10000,\"bwLoss\":0.001000,\"mtu\":1454,\"rtt\":5,\"ports\":[53,2053]},\"slotId\":1,\"appSpecification\":{\"minFps\":60,\"minBandwidth\":0,\"extTitleId\":\"ps3\",\"version\":1,\"timeLimit\":1,\"startTimeout\":100,\"afkTimeout\":100,\"afkTimeoutDisconnect\":100},\"konan\":{\"ps3AccessToken\":\"accessToken\",\"ps3RefreshToken\":\"refreshToken\"},\"requestGameSpecification\":{\"model\":\"bravia_tv\",\"platform\":\"android\",\"audioChannels\":\"5.1\",\"language\":\"sp\",\"acceptButton\":\"X\",\"connectedControllers\":[\"xinput\",\"ds3\",\"ds4\"],\"yuvCoefficient\":\"bt601\",\"videoEncoderProfile\":\"hw4.1\",\"audioEncoderProfile\":\"audio1\"},\"userProfile\":{\"onlineId\":\"psnId\",\"npId\":\"npId\",\"region\":\"US\",\"languagesUsed\":[\"en\",\"jp\"]},\"handshakeKey\":\"ESIzRFVmd4iZABEiM0RVZg==\"}\u0000";
        readonly byte[] handshakeKey = HexUtil.Unhexlify("11223344556677889900112233445566");
        readonly string sessionId = "sessionId4321";

        public LaunchSpecificationTests()
        {
        }
        
        [Fact]
        public void TestSerialization()
        {
            var launchSpecs = LaunchSpecification.GetStandardSpecs(sessionId, handshakeKey);
            var assembled = launchSpecs.Serialize();

            //Assert.Equal(expectedLaunchSpec.Length, assembled.Length);
            Assert.Equal(expectedLaunchSpec, assembled);
        }

        [Fact]
        public void TestDeserialization()
        {
            var deserialized = LaunchSpecification.Deserialize(expectedLaunchSpec);

            //Assert.Equal(handshakeKey, deserialized.HandshakeKey);
            Assert.Equal(sessionId, deserialized.SessionId);
        }
    }
}