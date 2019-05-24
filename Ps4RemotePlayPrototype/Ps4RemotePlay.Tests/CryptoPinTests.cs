using System;
using System.Text;
using Xunit;

using Ps4RemotePlay.Util;
using Ps4RemotePlay.Protocol.Crypto;

namespace Ps4RemotePlay.Tests
{
    public class CryptoPinTests
    {
        const int Pin = 4321;
        readonly byte[] Nonce = HexUtil.Unhexlify("00010203040506079998979695949392");

        Session _ctx;

        public CryptoPinTests()
        {
            _ctx = CryptoService.GetSessionForPin(Pin, Nonce);
        }

        [Fact]
        public void TestSmallBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob"); // 13 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("b582bcf9e63f6754e3e0efb382"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }

        [Fact]
        public void TestFullBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob123"); // 16 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("b582bcf9e63f6754e3e0efb3824d2b2e"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }

        [Fact]
        public void TestBigBlock()
        {
            var plaintext = Encoding.UTF8.GetBytes("PlainTextBlob12309876543210"); // 27 bytes

            var ciphertext = _ctx.Encrypt(plaintext);
            var verifyPlain = _ctx.Decrypt(ciphertext);

            Assert.Equal(plaintext.Length, ciphertext.Length);
            Assert.Equal(HexUtil.Unhexlify("b582bcf9e63f6754e3e0efb3824d2b2e180d1c6b097685e57c2fbb"), ciphertext);
            Assert.Equal(plaintext, verifyPlain);
        }
    }
}
