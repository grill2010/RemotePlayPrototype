using System;
using System.Text;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Xunit;

using Ps4RemotePlay.Util;
using Ps4RemotePlay.Protocol.Crypto;

namespace Ps4RemotePlay.Tests
{
    public class CryptoEcdhTests
    {
        readonly byte[] pubKeyBytes = HexUtil.Unhexlify("3059301306072A8648CE3D020106082A8648CE3D03010703420004ADA690571EBB9BD903B16A246D43A9E1F82A19D690BE69C10A9D78085B6B9724B8FF4042051D20E41C68DB8F205E317BA8EA64ECC482B154B30A604B420463EE");
        readonly AsymmetricCipherKeyPair _keyPair;
        readonly Session _ctx;

        public CryptoEcdhTests()
        {
            _keyPair = CryptoService.GenerateEcdhKeyPair();
            _ctx = CryptoService.GetSessionForEcdh(_keyPair, pubKeyBytes);
        }

        [Fact]
        public void TestPublicKeyBytesConversion()
        {
            var bytes = Session.GetPublicKeyBytesFromKeyPair(_keyPair);

            Assert.Equal(0x04, bytes[0]);
            Assert.Equal(65, bytes.Length);
        }
    }
}
