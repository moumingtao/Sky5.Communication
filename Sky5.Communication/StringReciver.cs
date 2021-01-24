using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sky5.Communication
{
    public abstract class StringReciver: SocketAsyncReciver
    {
        public Decoder Decoder;
        char[] chars;

        public virtual int CacheChars
        {
            get
            {
                var count = Encoding.GetMaxCharCount(BufferSize);
                if (count < 1) return 1;
                if (count > 64) return 64;
                return count;
            }
        }
        public override void BeginReceive(Socket socket)
        {
            if (chars == null)
            {
                chars = new char[CacheChars];
            }
            if (Decoder == null)
                Decoder = Encoding.GetDecoder();
            else
                Decoder.Reset();
            base.BeginReceive(socket);
        }
        public override bool ContinueRecv(Socket socket, SocketAsyncEventArgs e)
        {
            int byteIndex = e.Offset;
            while (true)
            {
                bool flush = (e.BytesTransferred == 0);
                Decoder.Convert(e.Buffer, byteIndex, e.BytesTransferred - byteIndex, chars, 0, chars.Length, flush, out int bytesUsed, out int charsUsed, out bool completed);
                if (charsUsed > 0)
                {
#if StringNotToReadOnlySpan
                    if (!ContinueRecv(e.RemoteEndPoint, chars, 0, charsUsed))
#else
                    var content = new ReadOnlySpan<char>(chars, 0, charsUsed);
                    if (!ContinueRecv(e.RemoteEndPoint, content))
#endif
                        return false;
                }
                byteIndex += bytesUsed;
                if (completed) return true;
            }
        }
#if StringNotToReadOnlySpan
        protected abstract bool ContinueRecv(EndPoint remote, char[] chars, int offset, int count);
#else
        protected abstract bool ContinueRecv(EndPoint remote, ReadOnlySpan<char> content);
#endif
    }
}
