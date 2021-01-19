using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public static class MessageHeader
    {
        public const int ByteSize = 6;
        public const int NumSize = 3;
        public static void Write(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)value;
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 16);
        }
        public static int ReadInt(byte[] buffer, ref int offset)
            => buffer[offset++] | (buffer[offset++] << 8) | (buffer[offset++] << 16);

        public static Dictionary<int, Type> CodeToType = new Dictionary<int, Type>();
        public static Dictionary<Type, int> TypeToCode = new Dictionary<Type, int>();
        public static void RegisterType(int code, Type type)
        {
            CodeToType.Add(code, type);
            TypeToCode.Add(type, code);
        }
    }
}
