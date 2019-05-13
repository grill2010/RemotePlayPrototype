using System;
using Org.BouncyCastle.Security;
using Ps4RemotePlayPrototype.Util;

namespace Ps4RemotePlayPrototype.Protocol.Crypto
{
    public static class CryptoService
    {
        public static readonly byte[] HmacKey = HexUtil.Unhexlify("AC078883C83A1FE811463AF39EE3E377");
        public static readonly byte[] RegAesKey = HexUtil.Unhexlify("3F1CC4B6DCBB3ECC50BAEDEF9734C7C9");
        public static readonly byte[] RegNonceKey = HexUtil.Unhexlify("E1EC9C3ADDBD0885FC0E1D789032C004");
        public static readonly byte[] AuthAesKey = RegNonceKey;
        public static readonly byte[] AuthNonceKey = HexUtil.Unhexlify("0149879B65398B394B3A8D48C30AEF51");


        public static Session GetSessionForPin(int pin)
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

            SecureRandom rnd = new SecureRandom();
            byte[] nonce = new byte[16];
            rnd.NextBytes(nonce);

            return new Session(key, nonce);
        }

        public static Session GetSessionForControl(byte[] rpKey, byte[] rpNonce)
        {
            byte[] key = new byte[CryptoService.AuthAesKey.Length];
            for (int i = 0; i < CryptoService.AuthAesKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte r = rpKey[i];
                byte k = CryptoService.AuthAesKey[i];
                key[i] = (byte)(n ^ k ^ (r + 0x34 - i) & 0xFF);
            }

            byte[] nonce = new byte[CryptoService.AuthNonceKey.Length];
            for (int i = 0; i < CryptoService.AuthNonceKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte k = CryptoService.AuthNonceKey[i];
                nonce[i] = (byte)(k ^ (0x200 + n - i - 0x27) & 0xFF);
            }

            return new Session(key, nonce);
        }


        /**************************/
        /*** debug test methods ***/
        /**************************/

        public static string GetRegistryAesKeyForPin(int pin)
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

            return HexUtil.Hexlify(key);
        }

        public static string GetSessionAesKeyForControl(byte[] rpKey, byte[] rpNonce)
        {
            byte[] key = new byte[CryptoService.AuthAesKey.Length];
            for (int i = 0; i < CryptoService.AuthAesKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte r = rpKey[i];
                byte k = CryptoService.AuthAesKey[i];
                key[i] = (byte)(n ^ k ^ (r + 0x34 - i) & 0xFF);
            }

            return HexUtil.Hexlify(key);
        }

        public static string GetSessionNonceValueForControl(byte[] rpNonce)
        {
            byte[] nonce = new byte[CryptoService.AuthNonceKey.Length];
            for (int i = 0; i < CryptoService.AuthNonceKey.Length; i++)
            {
                byte n = rpNonce[i];
                byte k = CryptoService.AuthNonceKey[i];
                nonce[i] = (byte)(k ^ (0x200 + n - i - 0x27) & 0xFF);
            }

            return HexUtil.Hexlify(nonce);
        }
    }
}
