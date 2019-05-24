using System.IO;

namespace Ps4RemotePlay.Protocol.Message
{
    interface IMessage
    {
        void Serialize(BinaryWriter binaryWriter);

        void Deserialize(BinaryReader binaryReader);
    }
}
