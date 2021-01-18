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
        WeakReference<SocketAsyncEventArgs> weakReference;
        public readonly Func<SocketAsyncEventArgs> Create;
        SocketAsyncEventArgs strongReference;

        public SocketAsyncEventArgsWeakReference(Func<SocketAsyncEventArgs> create)
        {
            weakReference = default;
            strongReference = default;
            Create = create;
        }
        public SocketAsyncEventArgs Value
        {
            get
            {
                if (strongReference == null)
                {
                    lock (Create)
                    {
                        if (strongReference == null)
                        {
                            if (weakReference == null)
                            {
                                strongReference = Create();
                                weakReference = new WeakReference<SocketAsyncEventArgs>(strongReference);
                            }
                            else if (!weakReference.TryGetTarget(out strongReference))// 被回收了，重新创建
                            {
                                strongReference = Create();
                                weakReference.SetTarget(strongReference);
                            }
                        }
                    }
                }
                return strongReference;
            }
        }
        public void Free()
        {
            strongReference = null;
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
