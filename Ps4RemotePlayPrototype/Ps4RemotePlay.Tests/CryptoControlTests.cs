using System;
using System.Text;
using Xunit;

using Ps4RemotePlay.Util;
using Ps4RemotePlay.Protocol.Crypto;

namespace Ps4RemotePlay.Tests
{
    public class CryptoControlTests
    {
        readonly byte[] rpKey = HexUtil.Unhexlify("22334455667788999988776655443322");
        readonly byte[] rpNonce = HexUtil.Unhexlify("01020304050607080910111213141516");

        Session _ctx;

        public CryptoControlTests()
        {
            _ctx = CryptoService.GetSessionForControl(rpKey, rpNonce);
        }

        [Fact]
        public void TestSmallBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob"); // 13 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("7b6f020a67990732125bcdc67e"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }

        [Fact]
        public void TestFullBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob123"); // 16 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("7b6f020a67990732125bcdc67ec907e1"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }

        [Fact]
        public void TestBigBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob12309876543210"); // 27 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("7b6f020a67990732125bcdc67ec907e142cd4f14838d7cf4aae380"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }
    }
}
