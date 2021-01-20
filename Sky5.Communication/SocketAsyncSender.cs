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
        volatile SendAble EmptyNode = new EmptySendAble();
        volatile SendAble First;
        volatile SendAble Last;
        class EmptySendAble : SendAble
        {
            public override void SetBuffer(SocketAsyncSender sender, byte[] buffer, ref int offset, ref bool flush, out bool completed)
            {
                flush = true;
                completed = true;
            }
        }
        object QueueLockObject => eventArgs.Create;
        public Encoding Encoding = Encoding.UTF8;
        SocketAsyncEventArgsWeakReference eventArgs;
        public SocketAsyncSender(Socket socket)
        {
            this.Socket = socket;
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
            while (true)
            {
                var last = Interlocked.CompareExchange(ref Last, s, null);// 只有消费者线程可以设置Last为null
                if (last == null)
                {
                    First = s;
                    while (eventArgs.Value != null)
                        Thread.Sleep(0);
                    eventArgs.Begin();
                    LoopSend(false, s, eventArgs.Value);
                    break;
                }

                if (Interlocked.CompareExchange(ref last.Next, s, null) == null)
                {
                    Last = s;
                    break;
                }
            }
        }
        volatile int offset;
        public bool AutoFlush;
        void LoopSend(bool sync, SendAble first, SocketAsyncEventArgs e)// 保证不被并行调用
        {
            while (true)
            {
                var flush = AutoFlush;
                first.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
                if (completed)
                {
                    First = first = first.Next;
                    if (first == null) flush = true;
                }
                if (flush || offset == e.Buffer.Length)
                {
                    e.SetBuffer(0, offset);
                    offset = 0;
                    if (!Socket.SendAsync(e))
                    {
                        if (sync)
                            OnCompleted(Socket, e);
                        else
                            ThreadPool.QueueUserWorkItem(state => OnCompleted(Socket, e));
                    }
                    else
                    { }
                    break;
                }
            }

        }

        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            var first = First;
            if (first == null)
            {
                while (true)
                {
                    var last = this.Last;
                    first = First;
                    if (first == null)
                    {
                        if (Interlocked.CompareExchange(ref this.Last, null, last) == last)
                        {
                            eventArgs.Free();
                            return;
                        }
                    }
                    else break;
                }
            }
            LoopSend(true, first, e);
        }
        #region IBufferWriter
        byte[] buffer;
        public void Advance(int count) => offset += count;
        public Memory<byte> GetMemory(int sizeHint) => new Memory<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        public Span<byte> GetSpan(int sizeHint) => new Span<byte>(eventArgs.Value.Buffer, offset, eventArgs.Value.Buffer.Length - offset);
        #endregion
    }
}
