using System;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Crypto
{
    public static class CryptoService
    {
        public static readonly byte[] HmacKey = HexUtil.Unhexlify("AC078883C83A1FE811463AF39EE3E377");
        public static readonly byte[] RegAesKey = HexUtil.Unhexlify("3F1CC4B6DCBB3ECC50BAEDEF9734C7C9");
        public static readonly byte[] RegNonceKey = HexUtil.Unhexlify("E1EC9C3ADDBD0885FC0E1D789032C004");
        public static readonly byte[] AuthAesKey = RegNonceKey;
        public static readonly byte[] AuthNonceKey = HexUtil.Unhexlify("0149879B65398B394B3A8D48C30AEF51");

        public static byte[] GetRandomNonce()
        {
            SecureRandom rnd = new SecureRandom();
            byte[] nonce = new byte[16];
            rnd.NextBytes(nonce);

            return nonce;
        }

        public static byte[] GetRegistryAesKeyForPin(int pin)
        {
            byte[] bytes = ByteUtil.IntToByteArray(pin);
            byte[] newByteArray = ByteUtil.ConcatenateArrays(bytes, new byte[12]);


            byte[] key = new byte[CryptoService.RegAesKey.Length];
            for (int i = 0; i < CryptoService.RegAesKey.Length; i++)
            {
                byte x = CryptoService.RegAesKey[i];
                byte y = newByteArray[i];
                key[i] = ((byte)(x ^ y));
            }

            return key;
        }

        public static byte[] GetSessionAesKeyForControl(byte[] rpKey, byte[] rpNonce)
        {
            byte[] key = new byte[CryptoService.AuthAesKey.Length];
            for (int i = 0; i < CryptoService.AuthAesKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte r = rpKey[i];
                byte k = CryptoService.AuthAesKey[i];
                key[i] = (byte)(n ^ k ^ (r + 0x34 - i) & 0xFF);
            }
            return key;
        }

        public static byte[] GetSessionNonceValueForControl(byte[] rpNonce)
        {
            byte[] nonce = new byte[CryptoService.AuthNonceKey.Length];
            for (int i = 0; i < CryptoService.AuthNonceKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte k = CryptoService.AuthNonceKey[i];
                nonce[i] = (byte)(k ^ (0x200 + n - i - 0x27) & 0xFF);
            }

            return nonce;
        }

        public static AsymmetricCipherKeyPair GenerateEcdhKeyPair()
        {
            return Session.GenerateKeyPair(X9ObjectIdentifiers.Prime256v1);
        }

        public static Session GetSessionForEcdh(AsymmetricCipherKeyPair own, byte[] foreignPubKey)
        {
            var sharedSecret = Session.GenerateSharedSecret(own.Private, foreignPubKey);

            var aesKey = new byte[16];
            var nonce = new byte[16];

            // TODO: Check if split of [16 bytes aes][16 bytes nonce] is correct
            Array.Copy(sharedSecret, 0, aesKey, 0, aesKey.Length);
            Array.Copy(sharedSecret, 16, nonce, 0, nonce.Length);

            return new Session(aesKey, nonce);
        }

        public static Session GetSessionForPin(int pin)
        {
            var key = GetRegistryAesKeyForPin(pin);
            var nonce = GetRandomNonce();
            return new Session(key, nonce);
        }

        public static Session GetSessionForControl(byte[] rpKey, byte[] rpNonce)
        {
            var key = GetSessionAesKeyForControl(rpKey, rpNonce);
            var nonce = GetSessionNonceValueForControl(rpNonce);

            return new Session(key, nonce);
        }
    }
}
