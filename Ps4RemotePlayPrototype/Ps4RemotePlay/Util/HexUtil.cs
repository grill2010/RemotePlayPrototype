using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ps4RemotePlay.Util
{
    public static class HexUtil
    {
        private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();

        /// <summary>
        /// Transform a byte array into a it's hexadecimal representation
        /// </summary>
        /// <param name="bytes">The bytes which should be converted</param>
        /// <returns>The representation in bytes</returns>
        public static string Hexlify(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xFF;
                hexChars[j * 2] = hexArray[(uint)v >> 4];
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }
            return new string(hexChars);
        }


        /// <summary>
        /// Transform a string of hexadecimal chars into a byte array
        /// </summary>
        /// <param name="argbuf">The string which should be converted</param>
        /// <returns>The representation in bytes</returns>
        public static byte[] Unhexlify(string argbuf)
        {
            int arglen = argbuf.Length;
            if (arglen % 2 != 0)
                throw new Exception("Odd-length string");

            byte[] retbuf = new byte[arglen / 2];

            for (int i = 0; i < arglen; i += 2)
            {
                int top = Convert.ToInt32(argbuf[i].ToString(), 16);
                int bot = Convert.ToInt32(argbuf[i + 1].ToString(), 16);
                if (top == -1 || bot == -1)
                    throw new Exception("Non-hexadecimal digit found");
                retbuf[i / 2] = (byte)((top << 4) + bot);
            }
            return retbuf;
        }
    }
}
