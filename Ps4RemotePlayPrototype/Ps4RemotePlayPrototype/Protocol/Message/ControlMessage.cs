using System.IO;

namespace Ps4RemotePlayPrototype.Protocol.Message
{
    public class ControlMessage : MessageBase
    {
        public int ReceiverId { get; set; }

        public int Crypto { get; set; }

        public int TagPos { get; set; }

        public byte Flag1 { get; set; }

        public byte ProtoBuffFlag { get; set; }

        public short PLoadSize { get; set; }

        public int FuncIncr { get; set; }

        public int ClassValue { get; set; }

        public byte[] UnParsedPayload = new byte[0];

        public ControlMessage() :base(0, 0)
        {
        }

        public ControlMessage(byte subType, int receiverId, int crypto, int tagPos, byte flag1, byte protoBuffFlag, short pLoadSize, int funcIncr, int classValue) : base(0, subType)
        {
            this.ReceiverId = receiverId;
            this.Crypto = crypto;
            this.TagPos = tagPos;
            this.Flag1 = flag1;
            this.ProtoBuffFlag = protoBuffFlag;
            this.PLoadSize = pLoadSize;
            this.FuncIncr = funcIncr;
            this.ClassValue = classValue;
        }

        protected override void SerializeMessage(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.ReceiverId);

            binaryWriter.Write(this.ReceiverId);
            binaryWriter.Write(this.Crypto);
            binaryWriter.Write(this.TagPos);
            binaryWriter.Write(this.Flag1);
            binaryWriter.Write(this.ProtoBuffFlag);
            binaryWriter.Write((short)this.PLoadSize);
            if (this.PLoadSize > 4)
            {
                binaryWriter.Write(this.FuncIncr);
                binaryWriter.Write(this.ClassValue);
            }
            if (this.UnParsedPayload.Length > 0)
            {
                binaryWriter.Write(this.UnParsedPayload);
            }
        }

        protected override void DeserializeMessage(BinaryReader binaryReader)
        {
            this.ReceiverId = binaryReader.ReadInt32();
            this.Crypto = binaryReader.ReadInt32();
            this.TagPos = binaryReader.ReadInt32();
            this.Flag1 = binaryReader.ReadByte();
            this.ProtoBuffFlag = binaryReader.ReadByte();
            this.PLoadSize = binaryReader.ReadInt16();
            if (this.PLoadSize >= 8)
            {
                this.FuncIncr = binaryReader.ReadInt32();
                this.ClassValue = binaryReader.ReadInt32();
            }
            if (this.ProtoBuffFlag == 1)
            {
                if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
                {
                    binaryReader.ReadByte(); // delimiter not used in payload
                    if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
                    {
                        this.UnParsedPayload = binaryReader.ReadBytes(int.MaxValue);
                    }
                }
            }
            if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
            {
                this.UnParsedPayload = binaryReader.ReadBytes(int.MaxValue);
            }
        }
    }
}
