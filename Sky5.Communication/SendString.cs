using System;
using System.Diagnostics;
using System.Text;

namespace Sky5.Communication
{
    public class SendString: SendAble
    {
        public readonly string Value;
        Encoder encoder;
        volatile int charIndex;

        public SendString(string value)
        {
            Value = value;
        }
#if TestSendNum
        volatile static int num;
#endif
        public
#if StringNotToReadOnlySpan
            unsafe
#endif
            override void SetBuffer(SocketAsyncSender sender, byte[] buffer, ref int offset, ref bool flush, out bool completed)
        {
            if (encoder == null)
                encoder = sender.Encoding.GetEncoder();
#if StringNotToReadOnlySpan
            fixed (char* chars = Value)
            fixed (byte* bytes = buffer)
            {
                encoder.Convert(chars + charIndex, Value.Length - charIndex, bytes + offset, buffer.Length - offset, Value.Length == charIndex, out int charsUsed, out int bytesUsed, out completed);
                charIndex += charsUsed;
                offset += bytesUsed;
            }
#else
            ReadOnlySpan<char> chars = Value;
            chars = chars.Slice(charIndex, Value.Length - charIndex);
            var bytes = new Span<byte>(buffer, offset, buffer.Length - offset);
            encoder.Convert(chars, bytes, Value.Length == charIndex, out int charsUsed, out int bytesUsed, out completed);
            charIndex += charsUsed;
            offset += bytesUsed;
#endif

#if TestSendNum
            if (num % 2 == 0)
                Debug.Assert(int.Parse(Value) == num / 2);
            else
                Debug.Assert(Value == "\r\n");
            if (completed) num++;
#endif
            if (buffer.Length - offset < sender.Encoding.GetMaxByteCount(1))
                flush = true;
        }
    }
}
