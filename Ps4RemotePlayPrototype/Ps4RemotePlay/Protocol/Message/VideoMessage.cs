using System;
using System.IO;
using Ps4RemotePlay.Protocol.Common;

namespace Ps4RemotePlay.Protocol.Message
{
    public class VideoMessage : MessageBase
    {
        public bool UseNulUnitInfoStruct { get; set; }

        public ushort PacketId { get; set; }

        public ushort FrameId { get; set; }

        public ushort UnitIndex { get; set; }

        public ushort TotalUnitsInFrame { get; set; }

        public ushort AdditionalUnitsInFrame { get; set; }

        public uint Codec { get; set; }

        public uint Unknown { get; set; }

        public byte AdaptiveStreamIndex { get; set; }

        public byte Unknown2 { get; set; }

        public uint TagPos { get; set; }

        public byte[] VideoPayload { get; set; }

        public VideoMessage() : base(2, 0)
        {
            this.VideoPayload = new byte[0];
        }

        protected override void SerializeMessage(BinaryWriter binaryWriter)
        {
            throw new NotImplementedException();
        }

        protected override void DeserializeMessage(BinaryReader binaryReader)
        {
            UseNulUnitInfoStruct = (((this.OriginValue >> 4) & 1) != 0);
            PacketId = binaryReader.ReadUInt16BE();
            FrameId = binaryReader.ReadUInt16BE();
            uint value = binaryReader.ReadUInt32BE();
            UnitIndex = (ushort)((value >> 0x15) & 0x7ff);
            TotalUnitsInFrame = (ushort)(((value >> 0xa) & 0x7ff) + 1);
            AdditionalUnitsInFrame = (ushort)(value & 0x3ff);
            Codec = binaryReader.ReadByte();
            uint skippBytes = binaryReader.ReadUInt32();
            TagPos = binaryReader.ReadUInt32BE();
            byte skippBytes2 = binaryReader.ReadByte();
            Unknown = binaryReader.ReadByte();
            byte value2 = binaryReader.ReadByte();
            AdaptiveStreamIndex = (byte) (value2 >> 5);
            Unknown2 = value2;

            if (UseNulUnitInfoStruct)
            {
                binaryReader.ReadByte();
                binaryReader.ReadByte();
                binaryReader.ReadByte();
            }

            if ((binaryReader.BaseStream.Length - binaryReader.BaseStream.Position) > 0)
            {
                long payloadSize = binaryReader.BaseStream.Length - binaryReader.BaseStream.Position;
                this.VideoPayload = binaryReader.ReadBytes((int)payloadSize);
            }
        }
    }
}