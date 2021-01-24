using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public abstract class SocketAsyncReciver
    {
        public Encoding Encoding = Encoding.UTF8;
        #region SocketAsyncEventArgs
        SocketAsyncEventArgs EventArgs;
        public int BufferSize = 1024 * 4;
        public ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
        #endregion

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            var socket = (Socket)sender;
        LOOP:
            if (ContinueRecv(socket, e))
            {
                if (!socket.ReceiveAsync(e))
                    goto LOOP;// 避免递归造成StackOverflowException
            }
            else
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }
                socket.Close();
                var buffer = EventArgs.Buffer;
                EventArgs.SetBuffer(null, 0, 0);
                BufferPool.Return(buffer);
            }
        }
        public abstract bool ContinueRecv(Socket socket, SocketAsyncEventArgs e);

        public virtual void BeginReceive(Socket socket)
        {
            if (EventArgs == null)
            {
                EventArgs = new SocketAsyncEventArgs();
                EventArgs.Completed += OnCompleted;
            }
            var buffer = BufferPool.Rent(BufferSize);
            EventArgs.SetBuffer(buffer, 0, buffer.Length);
            if (!socket.ReceiveAsync(EventArgs))
                OnCompleted(socket, EventArgs);
        }
    }
}
