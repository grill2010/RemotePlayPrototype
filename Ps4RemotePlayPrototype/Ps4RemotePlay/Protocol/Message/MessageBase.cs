using System.IO;

namespace Ps4RemotePlay.Protocol.Message
{
    public abstract class MessageBase : IMessage
    {
        protected byte OriginValue { get; set; }

        private byte Type { get; set; }

        private byte SubType { get; set; }


        protected MessageBase(byte type, byte subType)
        {
            this.Type = type;
            this.SubType = subType;
        }

        protected abstract void SerializeMessage(BinaryWriter binaryWriter);

        protected abstract void DeserializeMessage(BinaryReader binaryReader);


        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.GetMergedTypeSubTypeByte());
            this.SerializeMessage(binaryWriter);
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            this.OriginValue = binaryReader.ReadByte();
            this.SetTypeAndSubTypeByByte(this.OriginValue);
            this.DeserializeMessage(binaryReader);
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        private byte GetMergedTypeSubTypeByte()
        {
            return (byte)((this.SubType << 4) + this.Type);
        }

        private void SetTypeAndSubTypeByByte(byte byteValue)
        {
            this.Type = (byte)(byteValue & 0x0F);
            this.SubType = (byte)(byteValue >> 4);
        }
    }
}
