using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SocketAsyncSender
    {
        public readonly Socket Socket;
        volatile SendAble First;
        volatile SendAble Last;
        object QueueLockObject => eventArgs.Create;
        public Encoding Encoding = Encoding.UTF8;
        SocketAsyncEventArgsWeakReference eventArgs;
        public SocketAsyncSender(Socket socket)
        {
            this.Socket = socket;
            eventArgs = new SocketAsyncEventArgsWeakReference(CreateEventArgs);
        }

        private SocketAsyncEventArgs CreateEventArgs()
        {
            var e = SocketAsyncEventArgsWeakReference.CreateByBytesBuffer();
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
                    Send(eventArgs.Value);
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
        void Send(SocketAsyncEventArgs e)// 保证不被并行调用
        {
        LOOP:
            var flush = AutoFlush;
            First.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
            if (completed)
                First = First.Next;
            if (flush || offset == e.Buffer.Length)
            {
                e.SetBuffer(0, offset);
                offset = 0;
                if (!Socket.SendAsync(e))
                    OnCompleted(Socket, e);
            }
            else if(First != null)
                goto LOOP;
        }

        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            lock (QueueLockObject)
            {
                if (First == null)
                {
                    Last = null;
                    eventArgs.Free();
                }
                else Send(e);
            }
        }
    }
}
