using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SocketAsyncSender: IBufferWriter<byte>
    {
        public readonly Socket Socket;
        volatile SendAble First;
        volatile SendAble Last;
        object QueueLockObject => eventArgs.Create;
        public Encoding Encoding = Encoding.UTF8;
        volatile SocketAsyncEventArgsWeakReference eventArgs;
        public SocketAsyncSender(Socket socket)
        {
            this.Socket = socket;
            if (socket.SocketType != SocketType.Stream)
                AutoFlush = true;
            eventArgs = new SocketAsyncEventArgsWeakReference(CreateEventArgs);
        }

        private SocketAsyncEventArgs CreateEventArgs(SocketAsyncEventArgsWeakReference sender)
        {
            var e = sender.CreateByBytesBuffer(1024 * 4);
            e.Completed += this.OnCompleted;
            offset = 0;
            return e;
        }
        /// <summary>
        /// 将要发送的内容加入到队列，此方法是线程安全的
        /// </summary>
        /// <param name="s">要发送的内容</param>
        public void Enqueue(SendAble s)
        {
            s.Next = null;
            lock (QueueLockObject)
            {
                if (First == null)
                {
                    Last = s;
                    First = s;
                    eventArgs.Begin();
                    ThreadPool.QueueUserWorkItem(state => Send(s, eventArgs.Value));
                }
                else
                {
                    Last.Next = s;
                    Last = s;
                }
            }
        }
        volatile int offset;
        public bool AutoFlush;

        void Send(SendAble first, SocketAsyncEventArgs e)// 保证不被并行调用
        {
            do
            {
                var flush = AutoFlush;
                first.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
                lock (QueueLockObject)
                {
                    if (completed)
                    {
                        First = first = first.Next;
                        if (first == null)
                            flush = true;
                    }
                    if (flush || offset == e.Buffer.Length)
                    {
                        e.SetBuffer(0, offset);
                        offset = 0;
                        if (!Socket.SendAsync(e))
                            OnCompleted(Socket, e);
                        return;
                    }
                }
            } while (first != null);
        }
        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            SendAble first;
            lock (QueueLockObject)
            {
                first = First;
                if (first == null)
                {
                    Last = null;
                    eventArgs.Free();
                }
            }
            if (first != null)
            {
                Send(first, e);
            }
        }
        #region IBufferWriter
        byte[] buffer;
        public void Advance(int count) => offset += count;
        public Memory<byte> GetMemory(int sizeHint) => new Memory<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        public Span<byte> GetSpan(int sizeHint) => new Span<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        #endregion
    }
}
