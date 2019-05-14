using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Ps4RemotePlayPrototype.Util;

namespace Ps4RemotePlayPrototype.Protocol.Crypto
{
    public class Session
    {
        private readonly byte[] _key;
        private readonly byte[] _nonce;

        private ulong _inputCtr;
        private ulong _outputCtr;

        public Session(byte[] key, byte[] nonce)
        {
            if (key.Length != 16)
                throw new InvalidDataException("key.Length != 16");
            if (nonce.Length != 16)
                throw new InvalidDataException("nonce.Length != 16");
            this._key = key;
            this._nonce = nonce;
            this._inputCtr = 0;
            this._outputCtr = 0;
        }

        public byte[] Encrypt(byte[] data)
        {
            try
            {
                byte[] iv = GetIV(_outputCtr);
                ++_outputCtr;
                return CreateAesCfbCipher(iv, doEncrypt: true).DoFinal(data);
            }
            catch (Exception)
            {
                // Ignore
            }
            return new byte[0];
        }

        public byte[] Encrypt(byte[] data, ulong ctr)
        {
            try
            {
                byte[] iv = this.GetIV(ctr);
                return CreateAesCfbCipher(iv, doEncrypt: true).DoFinal(data);
            }
            catch (Exception)
            {
                // Ignore
            }
            return new byte[0];
        }

        public byte[] Decrypt(byte[] data)
        {
            try
            {
                byte[] iv = GetIV(_inputCtr);
                ++_inputCtr;

                return CreateAesCfbCipher(iv, doEncrypt: false).DoFinal(data);
            }
            catch (Exception)
            {
                // Ignore
            }
            return new byte[0];
        }

        public byte[] Decrypt(byte[] data, ulong ctr)
        {
            try
            {
                byte[] iv = GetIV(ctr);

                return CreateAesCfbCipher(iv, doEncrypt: false)
                    .DoFinal(data);
            }
            catch (Exception)
            {
                // Ignore
            }
            return new byte[0];
        }

        public byte[] GetNonceDerivative()
        {
            byte[] deriv = new byte[this._nonce.Length]; // ToDo check nonce length etc
            for (int i = 0; i < this._nonce.Length; i++)
            {
                deriv[i] = (byte)(CryptoService.RegNonceKey[i] ^ (0x200 + this._nonce[i] - i - 0x29) & 0xFF);
            }
            return deriv;
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        private byte[] GetIV(ulong counter)
        {
            byte[] counterBuffer = ByteUtil.ULongToByteArray(counter);
            byte[] hmacInput = ByteUtil.ConcatenateArrays(this._nonce, counterBuffer);

            byte[] hash = MacUtilities.CalculateMac("HMAC-SHA256", new KeyParameter(CryptoService.HmacKey), hmacInput);
            // Only take 16 bytes of calculated HMAC
            Array.Resize(ref hash, 16);
            return hash;
        }

        public IBufferedCipher CreateAesCfbCipher(byte[] iv, bool doEncrypt)
        {
            IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CFB/NoPadding");

            var keyParams = ParameterUtilities.CreateKeyParameter("AES", _key);
            var paramsWithIv = new ParametersWithIV(keyParams, iv);

            cipher.Init(doEncrypt, paramsWithIv);
            return cipher;
        }
    }
}
