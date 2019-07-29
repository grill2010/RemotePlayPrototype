using System;

namespace Ps4RemotePlay.Protocol.Crypto
{
    public class GmacInfo
    {
        public byte[] KeyBase { get; private set; }

        public byte[] Iv { get; private set; }

        public byte[] KeyGmacBase { get; private set; }

        public byte[] KeyGmacCurrent { get; set; }

        public long GmacCurrentIndex { get; set; }

        public GmacInfo(byte[] keyBase, byte[] iv, byte[] keyGmacBase, int gmacCurrentIndex)
        {
            this.KeyBase = keyBase;
            this.Iv = iv;
            this.KeyGmacBase = keyGmacBase;
            this.KeyGmacCurrent = keyGmacBase;
            this.GmacCurrentIndex = gmacCurrentIndex;
        }
    }
}
