using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public abstract class SendAble
    {
        internal SendAble Next;
        public abstract void SetBuffer(SocketAsyncSender sender, ref byte[] buffer, ref int offset, ref bool flush, out bool completed);
    }
    public class SendString: SendAble
    {
        public readonly string Value;
        Encoder encoder;
        int charIndex;

        public SendString(string value)
        {
            Value = value;
        }

        public override void SetBuffer(SocketAsyncSender sender, ref byte[] buffer, ref int offset, ref bool flush, out bool completed)
        {
            if(encoder == null)
                encoder = sender.Encoding.GetEncoder();
            ReadOnlySpan<char> chars = Value;
            chars = chars.Slice(charIndex, Value.Length - charIndex);
            var bytes = new Span<byte>(buffer, offset, buffer.Length - offset);
            encoder.Convert(chars, bytes, Value.Length == charIndex, out int charsUsed, out int bytesUsed, out completed);
            charIndex += charsUsed;
            offset += bytesUsed;
            if (buffer.Length - offset < sender.Encoding.GetMaxByteCount(1))
                flush = true;
        }
    }
}
