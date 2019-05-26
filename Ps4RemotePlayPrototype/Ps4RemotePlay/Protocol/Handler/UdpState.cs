using System.Net.Sockets;

namespace Ps4RemotePlay.Protocol.Handler
{
    class UdpState
    {
        // Size of receive buffer.
        public const int BufferLength = 65000;

        // Receive buffer.
        public readonly byte[] buffer = new byte[BufferLength];

        // Client  socket.
        public Socket udpClient = null;
    }
}
