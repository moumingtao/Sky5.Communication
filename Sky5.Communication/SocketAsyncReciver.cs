using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SocketAsyncReciver
    {
        SocketAsyncEventArgsWeakReference eventArgs;
        public Encoding Encoding = Encoding.UTF8;
        public int BufferSize;
        public SocketAsyncReciver(int bufferSize = 1024 * 4)
        {
            this.BufferSize = bufferSize;
            eventArgs = new SocketAsyncEventArgsWeakReference(CreateEventArgs);
        }
        private SocketAsyncEventArgs CreateEventArgs()
        {
            var e = SocketAsyncEventArgsWeakReference.CreateByBytesBuffer();
            e.Completed += this.OnCompleted;
            return e;
        }

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            var socket = (Socket)sender;
            if (ContinueRecv(socket, e))
            {
                if (!socket.ReceiveAsync(e))
                    OnCompleted(socket, e);
            }
            else
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }
                socket.Close();
                eventArgs.Free();
            }
        }
        public virtual bool ContinueRecv(Socket socket, SocketAsyncEventArgs e) => true;

        public virtual void BeginReceive(Socket socket)
        {
            var e = eventArgs.Value;
            if (!socket.ReceiveAsync(e))
                OnCompleted(socket, e);
        }
    }
    public abstract class StringReciver: SocketAsyncReciver
    {
        public Decoder Decoder;
        char[] chars;

        public override void BeginReceive(Socket socket)
        {
            if (chars == null)
            {
                var count = Encoding.GetMaxCharCount(BufferSize);
                if (count < 1) count = 1;
                if (count > 64) count = 64;
                chars = new char[count];
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
                    var content = new ReadOnlySpan<char>(chars, 0, charsUsed);
                    if (!ContinueRecv(e.RemoteEndPoint, content))
                        return false;
                }
                byteIndex += bytesUsed;
                if (completed) break;
            }
            return true;
        }

        protected abstract bool ContinueRecv(EndPoint remote, ReadOnlySpan<char> content);
    }
    public class SplitStringReciver : StringReciver
    {
        StringBuilder sb = new StringBuilder();
        public string Spliter = "\r\n";
        volatile int findIndex;
        protected override bool ContinueRecv(EndPoint remote, ReadOnlySpan<char> content)
        {
            sb.Append(content);
            var end = sb.Length - Spliter.Length;
            while (findIndex <= end)
            {
                if (Find())
                {
                    var line = sb.ToString(0, findIndex);
                    sb.Remove(0, line.Length + Spliter.Length);
                    findIndex = 0;
                    if (!ContinueLine(remote, line))
                        return false;
                    end = sb.Length - Spliter.Length;
                }
                else
                    findIndex++;
            }
            return true;
        }
        bool Find()
        {
            for (int i = 0; i < Spliter.Length; i++)
            {
                if (sb[findIndex + i] != Spliter[i])
                    return false;
            }
            return true;
        }
        protected virtual bool ContinueLine(EndPoint remote, string content) => true;
    }
}
