using System.IO;
using ProtoBuf;

namespace Ps4RemotePlay
{
    public static class ProtobufUtil
    {
        public static byte[] Serialize<T>(T message)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize<T>(stream, message);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            return Serializer.Deserialize<T>(new MemoryStream(data));
        }
    }
}
