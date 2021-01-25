using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class MessageHeader
    {
        public const int NumSize = 3;
        public static void Write(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)value;
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 16);
        }
        public static int ReadInt(byte[] buffer, ref int offset)
            => buffer[offset++] | (buffer[offset++] << 8) | (buffer[offset++] << 16);
        public bool Recv(ref ushort field)
        {
            if (field < 0)
            {
                if (len < MessageHeader.NumSize)
                {
                    EnsureBuffer(MessageHeader.NumSize);
                    return true;
                }
                else
                {
                    field = MessageHeader.ReadInt(e.Buffer, ref begin);
                    len -= MessageHeader.NumSize;
                    return false;
                }
            }
            return false;
        }

    }
}
