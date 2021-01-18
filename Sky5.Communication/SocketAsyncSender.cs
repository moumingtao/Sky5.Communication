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
        readonly Socket socket;
        volatile SendAble First;
        volatile SendAble Last;
        public Encoding Encoding = Encoding.UTF8;
        SocketAsyncEventArgsWeakReference eventArgs;
        public SocketAsyncSender(Socket socket)
        {
            this.socket = socket;
            eventArgs = new SocketAsyncEventArgsWeakReference(CreateEventArgs);
        }

        private SocketAsyncEventArgs CreateEventArgs()
        {
            var e = SocketAsyncEventArgsWeakReference.CreateByBytesBuffer();
            e.Completed += this.OnCompleted;
            offset = 0;
            return e;
        }

        public void Send(SendAble s)
        {
            s.Next = null;
            if (First == null)
            {
                lock (this)
                {
                    if (First == null)
                    {
                        Last = s;
                        First = s;
                        Send(eventArgs.Value);
                        return;
                    }
                }
            }
            Last.Next = s;
            Last = s;
        }
        volatile int offset;
        public bool AutoFlush;
        void Send(SocketAsyncEventArgs e)// 保证不被并行调用
        {
        LOOP:
            var flush = AutoFlush;
            var s = First;
            s.SetBuffer(this, e.Buffer, ref offset, ref flush, out bool completed);
            if (completed)
                First = s.Next;
            if (flush || offset == e.Buffer.Length)
            {
                e.SetBuffer(0, offset);
                offset = 0;
                if (!socket.SendAsync(e))
                    OnCompleted(socket, e);
            }
            else if(First != null)
                goto LOOP;
        }

        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (First == null)
            {
                lock (this)
                {
                    if (First == null)
                    {
                        Last = null;
                        eventArgs.Free();
                    }
                }
            }
            else
                Send(e);
        }
    }
}
