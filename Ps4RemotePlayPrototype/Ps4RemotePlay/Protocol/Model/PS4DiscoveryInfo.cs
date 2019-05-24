using System.Net;

namespace Ps4RemotePlay.Protocol.Model
{
    public class PS4DiscoveryInfo
    {
        public int Status { get; }

        public IPEndPoint Ps4EndPoint { get; }

        public string RawResponseData { get; }

        public PS4DiscoveryInfo(int status, IPEndPoint ps4EndPoint, string rawResponseData)
        {
            this.Status = status;
            this.Ps4EndPoint = ps4EndPoint;
            this.RawResponseData = rawResponseData;
        }
    }
}
