using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public struct SocketAsyncEventArgsWeakReference
    {
        WeakReference<SocketAsyncEventArgs> bufferReference;
        Func<SocketAsyncEventArgs> Create;

        public SocketAsyncEventArgsWeakReference(Func<SocketAsyncEventArgs> create)
        {
            bufferReference = default;
            Create = create;
        }

        public SocketAsyncEventArgs GetValue()
        {
            SocketAsyncEventArgs e;
            if (bufferReference == null)
            {
                e = Create();
                bufferReference = new WeakReference<SocketAsyncEventArgs>(e);
            }
            else if (!bufferReference.TryGetTarget(out e))// 被回收了，重新创建
            {
                e = Create();
                bufferReference.SetTarget(e);
            }
            return e;
        }
        public static SocketAsyncEventArgs CreateByBytesBuffer(int bufferSize = 1024 * 4)
        {
            var e = new SocketAsyncEventArgs();
            var buffer = new byte[bufferSize];
            e.SetBuffer(buffer, 0, buffer.Length);
            return e;
        }
    }
}
