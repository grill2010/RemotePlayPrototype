using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Net;
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

        private const int GmacKeyRefreshIvOffset = 44910;
        private const int GmacKeyRefreshKeyPosition = 45000;


        public static string GetUniqueKey(int size)
        {
            char[] chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
            byte[] data = new byte[size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

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
            return Session.GenerateKeyPair();
        }

        public static Session GetSessionForPin(int pin, byte[] nonce = null)
        {
            if (nonce != null && nonce.Length != 16)
                throw new InvalidKeyException("Nonce of invalid length");

            var key = GetRegistryAesKeyForPin(pin);

            if (nonce == null)
                nonce = GetRandomNonce();

            return new Session(key, nonce);
        }

        public static Session GetSessionForControl(byte[] rpKey, byte[] rpNonce)
        {
            var key = GetSessionAesKeyForControl(rpKey, rpNonce);
            var nonce = GetSessionNonceValueForControl(rpNonce);

            return new Session(key, nonce);
        }

        public static GmacInfo SetUpGmac(byte index, byte[] handshakeKey, byte[] ecdhSecret)
        {
            byte[] data = new byte[3 + handshakeKey.Length + 2];
            data[0] = 1;
            data[1] = index;
            data[2] = 0;
            Array.Copy(handshakeKey, 0, data, 3, handshakeKey.Length);
            data[3 + handshakeKey.Length + 0] = 1;
            data[3 + handshakeKey.Length + 1] = 0;

            byte[] hmac = Session.CalculateHMAC(ecdhSecret, data);
            byte[] keyBase = new byte[16];
            byte[] iv = new byte[16];
            Array.Copy(hmac, 0, keyBase, 0, 16);
            Array.Copy(hmac, 16, iv, 0, 16);

            byte[] gmacKey = GenerateGmacKey( keyBase, iv, 0);

            return new GmacInfo(keyBase, iv, gmacKey, 0);
        }

        public static byte[] AddGmacToBuffer(GmacInfo gmacInfo, uint keyPos, byte[] buffer, int gmacPositionInBuffer)
        {
            //keyPos = (uint) System.Net.IPAddress.HostToNetworkOrder((int)keyPos);

            byte[] iv = new byte[16];
            SetGmacCounter(iv, gmacInfo.Iv, keyPos / 0x10);

            byte[] gmacKey = gmacInfo.KeyGmacCurrent;
            long keyIndex = (keyPos > 0 ? keyPos - 1 : 0) / GmacKeyRefreshKeyPosition;

            if (keyIndex > gmacInfo.GmacCurrentIndex)
            {
                gmacKey = GenerateNewGmacKey(gmacInfo, keyIndex);
                gmacInfo.KeyGmacCurrent = gmacKey;
                gmacInfo.GmacCurrentIndex = keyIndex;
                gmacKey = gmacInfo.KeyGmacCurrent;
            }
            else if (keyIndex < gmacInfo.GmacCurrentIndex)
            {
                gmacKey = GenerateTmpGmacKey(gmacInfo, keyIndex);
            }

            IMac mac = new GMac(new GcmBlockCipher(new AesEngine()), 32);
            ICipherParameters key = new KeyParameter(gmacKey);
            mac.Init(new ParametersWithIV(key, iv));
            mac.BlockUpdate(buffer, 0, buffer.Length);
            int macSize = mac.GetMacSize();
            byte[] gmac = new byte[macSize];
            mac.DoFinal(gmac, 0);

            for (int i = 0; i < gmac.Length; i++)
            {
                buffer[gmacPositionInBuffer + i] = gmac[i];
            }

            return gmac;
        }

        public static byte[] EncryptGmacMessage(GmacInfo gmacInfo, uint keyPos, byte[] buffer)
        {
            return GmacCrypto(gmacInfo, keyPos, buffer);
        }

        public static byte[] DecryptGmacMessage(GmacInfo gmacInfo, uint keyPos, byte[] buffer)
        {
            return GmacCrypto(gmacInfo, keyPos, buffer);
        }

        /************************/
        /**** private methods ***/
        /************************/

        private static byte[] GmacCrypto(GmacInfo gmacInfo, uint keyPos, byte[] buffer)
        {
            uint padding = keyPos % 16;
            int size = (((int)padding + buffer.Length + 16 - 1) / 16) * 16;

            byte[] keyStream = GenerateKeyStream(gmacInfo, keyPos - padding, size);

            byte[] decryptedData = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                decryptedData[i] = (byte)(buffer[i] ^ keyStream[padding + i]);
            }

            return decryptedData;
        }

        private static void SetGmacCounter(byte[] source, byte[] baseValue, long offset)
        {
            int i = 0;
            do
            {
                long value = baseValue[i] + offset;
                source[i] = (byte) (value & 0xff);
                offset = (int) (value >> 8);
                i++;
            } while (i < 16 && offset != 0);

            if (i < 16)
            {
                Array.Copy(baseValue, i, source, i, 16 - i);
            }
        }

        private static byte[] GenerateGmacKey( byte[] keyBase, byte[] iv, long index)
        {
            byte[] data = new byte[32];
            Array.Copy(keyBase, 0, data, 0, 16);
            byte[] gmacCounter = new byte[16];
            SetGmacCounter(gmacCounter, iv, index * GmacKeyRefreshIvOffset);
            Array.Copy(gmacCounter, 0, data, 16, 16);

            byte[] md = Session.CalculateHash(data);
            for (int i = 0; i < 16; i++)
            {
                md[i] = (byte)(md[i] ^ md[i + 16]);
            }

            byte[] gmacKey = new byte[16];
            Array.Copy(md, 0, gmacKey, 0, 16);
            return gmacKey;
        }

        public static byte[] GenerateNewGmacKey(GmacInfo gmacInfo, long index )
        {
            return GenerateGmacKey(gmacInfo.KeyGmacBase, gmacInfo.Iv, index);
        }

        private static byte[] GenerateTmpGmacKey(GmacInfo gmacInfo, long index)
        {
            if (index != 0)
            {
                return GenerateGmacKey(gmacInfo.KeyGmacBase, gmacInfo.Iv, index);
            }
            else
            {
                return gmacInfo.KeyGmacCurrent;
            }
        }

        public static byte[] GenerateKeyStream(GmacInfo gmacInfo, uint keyPos, int keyStreamSize)
        {
            var cipher = CipherUtilities.GetCipher("AES/ECB/NoPadding");
            cipher.Init(true, new KeyParameter(gmacInfo.KeyBase));

            int counterOffset = (int)keyPos / 16;

            byte[] keyStream = new byte[keyStreamSize];
            for (int i = 0; i < keyStreamSize; i += 16)
            {
                byte[] keyStreamPart = new byte[16];
                SetGmacCounter(keyStreamPart, gmacInfo.Iv, counterOffset++);
                Array.Copy(keyStreamPart, 0, keyStream, i, keyStreamPart.Length);
            }

            return cipher.DoFinal(keyStream);
        }
    }
}
