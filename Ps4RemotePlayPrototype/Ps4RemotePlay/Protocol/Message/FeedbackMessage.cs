using System.IO;
using Ps4RemotePlay.Protocol.Common;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Message
{
    public class FeedbackMessage : MessageBase
    {
        public ushort AckId { get; set; }

        public byte Empty { get; set; }

        public uint TagPos { get; set; }

        public uint Crypto { get; set; }

        public byte[] ControllerPayload { get; set; }

        public FeedbackMessage() : base(6, 0)
        {
            this.ControllerPayload = new byte[0];
        }

        public FeedbackMessage(ushort ackId, byte empty, uint tagPos, uint crypto) : base(6, 0)
        {
            this.AckId = ackId;
            this.Empty = empty;
            this.TagPos = tagPos;
            this.Crypto = crypto;
            this.ControllerPayload = new byte[0];
        }

        protected override void SerializeMessage(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(ByteUtil.IntToShortByteArray(this.AckId));
            binaryWriter.Write(Empty);
            binaryWriter.Write(ByteUtil.UIntToByteArray(this.TagPos));
            binaryWriter.Write(ByteUtil.UIntToByteArray(this.Crypto));
            binaryWriter.Write(this.ControllerPayload);
        }

        protected override void DeserializeMessage(BinaryReader binaryReader)
        {
            AckId = binaryReader.ReadUInt16BE();
            Empty = binaryReader.ReadByte();
            TagPos = binaryReader.ReadUInt32BE();
            Crypto = binaryReader.ReadUInt32BE();

            if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
            {
                long payloadSize = binaryReader.BaseStream.Length - binaryReader.BaseStream.Position;
                this.ControllerPayload = binaryReader.ReadBytes((int)payloadSize);
            }
        }
    }
}