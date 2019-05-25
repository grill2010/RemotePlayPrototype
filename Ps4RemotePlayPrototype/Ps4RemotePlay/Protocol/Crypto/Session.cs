using System;
using System.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Crypto
{
    public class Session
    {
        private static readonly DerObjectIdentifier ECCurveAlgo = SecObjectIdentifiers.SecP256k1;
        private const string KeyExchangeAlgorithm = "ECDH";

        private readonly byte[] _key;
        private readonly byte[] _nonce;

        private ulong _inputCtr;
        private ulong _outputCtr;

        public static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var gen = new ECKeyPairGenerator();
            var genParams = new ECKeyGenerationParameters(ECCurveAlgo, new SecureRandom());

            gen.Init(genParams);
            return gen.GenerateKeyPair();
        }

        public static byte[] GetPublicKeyBytesFromKeyPair(AsymmetricCipherKeyPair keyPair)
        {
            return ((ECPublicKeyParameters)keyPair.Public).Q.GetEncoded();
        }

        public static ICipherParameters ConvertPubkeyBytesToCipherParams(byte[] pubKeyBytes)
        {
            X9ECParameters ecCurve = ECNamedCurveTable.GetByOid(ECCurveAlgo);
            ECPoint point = ecCurve.Curve.DecodePoint(pubKeyBytes);
            return new ECPublicKeyParameters(KeyExchangeAlgorithm, point, ECCurveAlgo);
        }

        public static byte[] GenerateSharedSecret(ICipherParameters clientPrivateKey, ICipherParameters foreignPublicKey)
        {
            var agreement = AgreementUtilities.GetBasicAgreement(KeyExchangeAlgorithm);
            agreement.Init(clientPrivateKey);

            var bytes = agreement.CalculateAgreement(foreignPublicKey).ToByteArrayUnsigned();
            return bytes;
        }

        public static byte[] CalculateHash(byte[] data)
        {
            return DigestUtilities.CalculateDigest("SHA256", data);
        }

        public static byte[] CalculateHMAC(byte[] key, byte[] data)
        {
            return MacUtilities.CalculateMac("HMAC-SHA256", new KeyParameter(key), data);
        }

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
            byte[] iv = GetIV(_outputCtr);
            ++_outputCtr;

            return CreateAesCfbCipher(iv, doEncrypt: true).DoFinal(data);
        }

        public byte[] Encrypt(byte[] data, ulong ctr)
        {
            byte[] iv = this.GetIV(ctr);
            return CreateAesCfbCipher(iv, doEncrypt: true).DoFinal(data);
        }

        public byte[] Decrypt(byte[] data)
        {
            byte[] iv = GetIV(_inputCtr);
            ++_inputCtr;

            return CreateAesCfbCipher(iv, doEncrypt: false).DoFinal(data);
        }

        public byte[] Decrypt(byte[] data, ulong ctr)
        {
            byte[] iv = GetIV(ctr);

            return CreateAesCfbCipher(iv, doEncrypt: false).DoFinal(data);
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

            byte[] hash = CalculateHMAC(CryptoService.HmacKey, hmacInput);
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
