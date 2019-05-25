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
        readonly byte[] pubKeyBytes = HexUtil.Unhexlify("044834A2D8F454CD8FBCD8F06DF751A46E3A3C0D0A5EBB2D3AA381C97ACD28FE8F693B6E5E2E58453E5920ED1916BFFB1B53A155E2FBDF537EB7F50FA5969C1762");
        readonly AsymmetricCipherKeyPair _keyPair;

        public CryptoEcdhTests()
        {
            _keyPair = CryptoService.GenerateEcdhKeyPair();
        }

        [Fact]
        public void TestPublicKeyBytesConversion()
        {
            var bytes = Session.GetPublicKeyBytesFromKeyPair(_keyPair);

            Assert.Equal(0x04, bytes[0]);
            Assert.Equal(65, bytes.Length);
        }

        [Fact]
        public void TestKeyDerivation()
        {
            var foreignKeyParams = Session.ConvertPubkeyBytesToCipherParams(pubKeyBytes);
            var sharedSecret = Session.GenerateSharedSecret(_keyPair.Private, foreignKeyParams);

            Assert.Equal(32, sharedSecret.Length);
        }
    }
}
