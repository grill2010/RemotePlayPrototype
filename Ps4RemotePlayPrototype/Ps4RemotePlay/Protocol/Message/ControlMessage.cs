using System.IO;
using Ps4RemotePlay.Protocol.Common;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Message
{
    public class ControlMessage : MessageBase
    {
        public uint ReceiverId { get; set; }

        public uint Crypto { get; set; }

        public uint TagPos { get; set; }

        public byte Flag1 { get; set; }

        public byte ProtoBuffFlag { get; set; }

        public ushort PLoadSize { get; set; }

        public uint FuncIncr { get; set; }

        public uint ClassValue { get; set; }

        public byte[] UnParsedPayload { get; set; }


        public ControlMessage() :base(0, 0)
        {
            this.UnParsedPayload = new byte[0];
        }

        public ControlMessage(byte subType, uint receiverId, uint crypto, uint tagPos, byte flag1, byte protoBuffFlag, ushort pLoadSize, uint funcIncr, uint classValue) : base(0, subType)
        {
            this.ReceiverId = receiverId;
            this.Crypto = crypto;
            this.TagPos = tagPos;
            this.Flag1 = flag1;
            this.ProtoBuffFlag = protoBuffFlag;
            this.PLoadSize = pLoadSize;
            this.FuncIncr = funcIncr;
            this.ClassValue = classValue;
            this.UnParsedPayload = new byte[0];
        }

        protected override void SerializeMessage(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(ByteUtil.UIntToByteArray(this.ReceiverId));
            binaryWriter.Write(ByteUtil.UIntToByteArray(this.Crypto));
            binaryWriter.Write(ByteUtil.UIntToByteArray(this.TagPos));
            binaryWriter.Write(this.Flag1);
            binaryWriter.Write(this.ProtoBuffFlag);
            binaryWriter.Write(ByteUtil.IntToShortByteArray((short)this.PLoadSize));
            if (this.PLoadSize > 4)
            {
                binaryWriter.Write(ByteUtil.UIntToByteArray(this.FuncIncr));
                binaryWriter.Write(ByteUtil.UIntToByteArray(this.ClassValue));
            }
            if (this.UnParsedPayload.Length > 0)
            {
                binaryWriter.Write(this.UnParsedPayload);
            }
        }

        protected override void DeserializeMessage(BinaryReader binaryReader)
        {
            this.ReceiverId = binaryReader.ReadUInt32BE();
            this.Crypto = binaryReader.ReadUInt32BE();
            this.TagPos = binaryReader.ReadUInt32BE();
            this.Flag1 = binaryReader.ReadByte();
            this.ProtoBuffFlag = binaryReader.ReadByte();
            this.PLoadSize = binaryReader.ReadUInt16BE();
            if (this.PLoadSize >= 8)
            {
                this.FuncIncr = binaryReader.ReadUInt32BE();
                this.ClassValue = binaryReader.ReadUInt32BE();
            }
            if (this.ProtoBuffFlag == 1)
            {
                if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
                {
                    binaryReader.ReadByte(); // delimiter not used in payload
                    if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
                    {
                        long payloadSize = binaryReader.BaseStream.Length - binaryReader.BaseStream.Position;
                        this.UnParsedPayload = binaryReader.ReadBytes((int) payloadSize);
                    }
                }
            }
            else if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
            {
                long payloadSize = binaryReader.BaseStream.Length - binaryReader.BaseStream.Position;
                this.UnParsedPayload = binaryReader.ReadBytes((int) payloadSize);
            }
        }
    }
}
