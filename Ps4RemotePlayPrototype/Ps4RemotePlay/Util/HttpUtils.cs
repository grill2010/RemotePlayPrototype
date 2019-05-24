using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ps4RemotePlay.Util
{
    public static class HttpUtils
    {
        public static HttpStatusCode GetStatusCode(string response)
        {
            string rawCode = response.Split(' ').Skip(1).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(rawCode) && rawCode.Length > 2)
            {
                rawCode = rawCode.Substring(0, 3);

                if (int.TryParse(rawCode, out var code))
                {
                    return (HttpStatusCode)code;
                }
            }

            return HttpStatusCode.Unused;
        }

        public static byte[] GetBodyPayload(byte[] response)
        {
            byte[] separators = { 0x0d, 0x0a, 0x0d, 0x0a };
            return ByteUtil.SeparateByteArrayBySequenceAndGetLastPart(response, separators);
        }

        public static Dictionary<string, string> SplitHttpResponse(string data)
        {
            string[] splitData = data.Split('\n');
            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
            for (int i = 1; i < splitData.Length; i++)
            {
                string pair = splitData[i];
                string[] keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    httpHeaders.Add(keyValue[0], keyValue[1]);
                }
            }

            return httpHeaders;
        }
    }
}
