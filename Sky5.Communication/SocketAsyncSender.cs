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
        SendAble First;
        SendAble Last;
        object QueueLockObject => eventArgs.Create;
        public Encoding Encoding = Encoding.UTF8;
        SocketAsyncEventArgsWeakReference eventArgs;
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
            bool begin;
            lock (QueueLockObject)
            {
                if (Last == null)
                {
                    Last = s;
                    First = s;
                    eventArgs.Begin();
                    begin = true;
                }
                else if (First == null)
                {
                    Last = s;
                    First = s;
                    begin = false;
                }
                else
                {
                    Last.Next = s;
                    Last = s;
                    begin = false;
                }
            }
            if(begin)
                ThreadPool.QueueUserWorkItem(state => Send(s, eventArgs.Value));
        }
        int offset;
        public bool AutoFlush;

        #region 这两个函数轮流交替执行，且不会被并行执行，(Send→OnCompleted)循环
        void Send(SendAble first, SocketAsyncEventArgs e)// 保证不被并行调用
        {
            #region 准备要发送的数据，写入缓冲区
            do
            {
                var flush = AutoFlush;
                first.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
                if (completed)
                {
                    First = first = first.Next;
                    if (first == null) break;
                }
                if (flush || offset == e.Buffer.Length)
                    break;
            } while (first != null);
            #endregion

            #region 执行发送
            e.SetBuffer(0, offset);
            offset = 0;
            if (!Socket.SendAsync(e))
                OnCompleted(Socket, e);
            #endregion
        }
        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            SendAble first;
            lock (QueueLockObject)
            {
                first = First;
                if (first == null)
                {
                    Last = null;// 以此作为数据推送停止的依据
                    eventArgs.Free();
                }
            }
            if (first != null)
                Send(first, e);
        }
        #endregion

        #region IBufferWriter
        byte[] buffer;
        public void Advance(int count) => offset += count;
        public Memory<byte> GetMemory(int sizeHint) => new Memory<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        public Span<byte> GetSpan(int sizeHint) => new Span<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        #endregion
    }
}
