using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ps4RemotePlay.Util
{
    public static class ByteUtil
    {

        /// <summary>
        /// Convert the integer to a short byte array.
        /// This conversion used big endian byte order.
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The representation in bytes</returns>
        public static byte[] IntToShortByteArray(int value)
        {
            byte b0 = (byte)((value & 0x0000FF00) >> 8);
            byte b1 = (byte)((value & 0x000000FF));

            return new byte[] { b0, b1 };
        }


        /// <summary>
        /// Convert the short byte array to an int value.
        /// This conversion used big endian byte order.</summary>
        /// <param name="b">The byte array to be converted</param>
        /// <returns>The representation as int</returns>
        public static int ShortByteArrayToInt(byte[] b)
        {
            return ((b[0] & 0xFF) << 8)
                    + (b[1] & 0xFF);
        }


        /// <summary>
        /// Convert the integer to a int byte array.
        /// This conversion used big endian byte order.
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The representation in bytes</returns>
        public static byte[] IntToByteArray(int value)
        {
            byte b0 = (byte)(value >> 24);
            byte b1 = (byte)((value & 0x00FF0000) >> 16);
            byte b2 = (byte)((value & 0x0000FF00) >> 8);
            byte b3 = (byte)((value & 0x000000FF));

            return new byte[] { b0, b1, b2, b3 };
        }

        public static byte[] UIntToByteArray(uint value)
        {
            byte b0 = (byte)(value >> 24);
            byte b1 = (byte)((value & 0x00FF0000) >> 16);
            byte b2 = (byte)((value & 0x0000FF00) >> 8);
            byte b3 = (byte)((value & 0x000000FF));

            return new byte[] { b0, b1, b2, b3 };
        }

        public static byte[] ULongToByteArray(ulong value)
        {
            byte b0 = (byte)(value >> 56);
            byte b1 = (byte)((value & 0x00FF000000000000) >> 48);
            byte b2 = (byte)((value & 0x0000FF0000000000) >> 40);
            byte b3 = (byte)((value & 0x000000FF00000000) >> 32);
            byte b4 = (byte)((value & 0x00000000FF000000) >> 24);
            byte b5 = (byte)((value & 0x0000000000FF0000) >> 16);
            byte b6 = (byte)((value & 0x000000000000FF00) >> 8);
            byte b7 = (byte)((value & 0x00000000000000FF));

            return new byte[] { b0, b1, b2, b3, b4, b5, b6, b7 };
        }


        /// <summary>
        /// Convert the int byte array to an int value.
        /// This conversion used big endian byte order.
        /// </summary>
        /// <param name="b">The byte array to be converted</param>
        /// <returns>This conversion used big endian byte order.</returns>
        public static int ByteArrayToInt(byte[] b)
        {
            return ((b[0] & 0xFF) << 24)
                    + ((b[1] & 0xFF) << 16)
                    + ((b[2] & 0xFF) << 8)
                    + (b[3] & 0xFF);
        }

        public static uint ByteArrayToUInt(byte[] b)
        {
            return (uint)(((b[0] & 0xFF) << 24)
                   + ((b[1] & 0xFF) << 16)
                   + ((b[2] & 0xFF) << 8)
                   + (b[3] & 0xFF));
        }


        /// <summary>
        /// Convert the integer to a single byte.
        /// This conversion used big endian byte order.
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The representation as byte</returns>
        public static byte IntToByte(int value)
        {
            return (byte)((value & 0x000000FF));
        }

        public static byte[] ConcatenateArrays(params byte[][] byteBuffers)
        {
            switch (byteBuffers.Length)
            {
                case 0:
                    return new byte[0];
                case 1:
                    return byteBuffers[0];
                default:
                    int offset = 0;
                    int totalSize = 0;
                    foreach (var byteBuffer in byteBuffers)
                    {
                        totalSize += byteBuffer.Length;
                    }

                    byte[] buffer = new byte[totalSize];
                    foreach (var byteBuffer in byteBuffers)
                    {
                        Buffer.BlockCopy(byteBuffer, 0, buffer, offset, byteBuffer.Length);
                        offset += byteBuffer.Length;
                    }

                    return buffer;
            }
        }

        private static byte[] ConcatenateArrays(List<byte[]> byteBuffers)
        {
            switch (byteBuffers.Count)
            {
                case 0:
                    return new byte[0];
                case 1:
                    return byteBuffers[0];
                default:
                    int offset = 0;
                    int totalSize = 0;
                    foreach (var byteBuffer in byteBuffers)
                    {
                        totalSize += byteBuffer.Length;
                    }

                    byte[] buffer = new byte[totalSize];
                    foreach (var byteBuffer in byteBuffers)
                    {
                        Buffer.BlockCopy(byteBuffer, 0, buffer, offset, byteBuffer.Length);
                        offset += byteBuffer.Length;
                    }

                    return buffer;
            }
        }

        public static byte[] HttpHeadersToByteArray(Dictionary<string, string> headers)
        {
            try
            {
                List<byte[]> buffers = new List<byte[]>();
                foreach (KeyValuePair<string, string> entry in headers)
                {

                    if (entry.Key != null && entry.Value != null)
                    {
                        string key = entry.Key.Trim();
                        string value = entry.Value.Trim();
                        byte[] keyBuffer = Encoding.ASCII.GetBytes(key);
                        byte[] separator = Encoding.ASCII.GetBytes(": ");
                        byte[] valueBuffer = Encoding.UTF8.GetBytes(value);
                        byte[] endingBuffer = Encoding.ASCII.GetBytes("\r\n");

                        buffers.Add(ByteUtil.ConcatenateArrays(keyBuffer, separator, valueBuffer, endingBuffer));
                    }
                }
                return ByteUtil.ConcatenateArrays(buffers);
            }
            catch (Exception)
            {
                // Ignore
            }
            return new byte[0];
        }

        public static Dictionary<string, string> ByteArrayToHttpHeader(byte[] data)
        {
            Dictionary<string, string> parsedHeaders = new Dictionary<string, string>();
            try
            {
                string headers = Encoding.UTF8.GetString(data);
                string[] lines = headers.Split(new[] {Environment.NewLine},StringSplitOptions.None);
                foreach (var line in lines)
                {
                    string[] header = line.Split(':');
                    if (header.Length == 2)
                    {
                        parsedHeaders.Add(header[0].Trim(), header[1].Trim());
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            return parsedHeaders;
        }

        public static byte[] SeparateByteArrayBySequenceAndGetLastPart(byte[] source, byte[] separator)
        {
            for (var i = 0; i < source.Length; ++i)
            {
                if (Equals(source, separator, i))
                {
                    var index = i + separator.Length;
                    var part = new byte[source.Length - index];
                    Buffer.BlockCopy(source, index, part, 0, part.Length);
                    return part;
                }
            }
           return new byte[0];
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        private static bool Equals(byte[] source, byte[] separator, int index)
        {
            for (int i = 0; i < separator.Length; ++i)
                if (index + i >= source.Length || source[index + i] != separator[i])
                    return false;
            return true;
        }
    }
}
