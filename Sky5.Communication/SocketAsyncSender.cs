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
        public Encoding Encoding = Encoding.UTF8;
        public MessageHeader Header;

        #region SocketAsyncEventArgs
        SocketAsyncEventArgs EventArgs = new SocketAsyncEventArgs();
        public int BufferSize = 1024 * 4;
        public ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
        #endregion

        public SocketAsyncSender(Socket socket)
        {
            this.Socket = socket;
            if (socket.SocketType != SocketType.Stream)
                AutoFlush = true;
            EventArgs.Completed += this.OnCompleted;
        }

        /// <summary>
        /// 将要发送的内容加入到队列，此方法是线程安全的
        /// </summary>
        /// <param name="s">要发送的内容</param>
        public void Enqueue(SendAble s)
        {
            s.Next = null;
            bool begin;
            lock (EventArgs)
            {
                if (Last == null)
                {
                    Last = s;
                    First = s;
                    var buffer = BufferPool.Rent(BufferSize);
                    EventArgs.SetBuffer(buffer, 0, buffer.Length);
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
                ThreadPool.QueueUserWorkItem(state => Send(s, EventArgs));
        }
        #region 这两个函数轮流交替执行，且不会被并行执行，(Send→OnCompleted)循环
        int offset;
        public bool AutoFlush;
        void Send(SendAble first, SocketAsyncEventArgs e)// 保证不被并行调用
        {
        LOOP:
            #region 准备要发送的数据，写入缓冲区
            do
            {
                var flush = AutoFlush;
                first.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
                if (completed)
                    first = first.Next;
                if (first == null || (flush && offset > 0) || offset == e.Buffer.Length)
                    break;
            } while (first != null);
            #endregion

            First = first;

            #region 执行发送
            e.SetBuffer(0, offset);
            offset = 0;
            if (!Socket.SendAsync(e) && ContinueSend(out first))
                goto LOOP;// 避免递归造成StackOverflowException
            #endregion
        }
        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (ContinueSend(out var first))
                Send(first, e);
        }
        bool ContinueSend(out SendAble first)
        {
            lock (EventArgs)
            {
                first = First;
                if (first == null)
                {
                    Last = null;// 以此作为数据推送停止的依据
                    var buffer = EventArgs.Buffer;
                    EventArgs.SetBuffer(null, 0, 0);
                    BufferPool.Return(buffer);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region IBufferWriter
        byte[] buffer;
        public void Advance(int count) => offset += count;
        public Memory<byte> GetMemory(int sizeHint) => new Memory<byte>(EventArgs.Buffer, offset, EventArgs.Buffer.Length - offset);
        public Span<byte> GetSpan(int sizeHint) => new Span<byte>(EventArgs.Buffer, offset, EventArgs.Buffer.Length - offset);
        #endregion
    }
}
