using System.IO;

namespace Ps4RemotePlayPrototype.Protocol.Message
{
    interface IMessage
    {
        void Serialize(BinaryWriter binaryWriter);

        void Deserialize(BinaryReader binaryReader);
    }
}
