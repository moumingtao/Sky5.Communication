using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SocketAsyncEventArgsWeakReference
    {
        WeakReference<SocketAsyncEventArgs> weakReference;
        public readonly Func<SocketAsyncEventArgsWeakReference, SocketAsyncEventArgs> Create;
        volatile SocketAsyncEventArgs value;
        public SocketAsyncEventArgs Value => value;
        ArrayPool<byte> buffers = ArrayPool<byte>.Shared;

        public SocketAsyncEventArgsWeakReference(Func<SocketAsyncEventArgsWeakReference, SocketAsyncEventArgs> create)
        {
            Create = create;
        }
        public void Begin()
        {
            if (value == null)
            {
                if (weakReference == null)
                {
                    value = Create(this);
                    weakReference = new WeakReference<SocketAsyncEventArgs>(value);
                }
                else if (!weakReference.TryGetTarget(out value))// 被回收了，重新创建
                {
                    value = Create(this);
                    weakReference.SetTarget(value);
                }
            }
        }

        public void Free()
        {
            if (value.Buffer != null)
                buffers.Return(value.Buffer);
            value = null;
        }
        public SocketAsyncEventArgs CreateByBytesBuffer(int minBufferSize)
        {
            var e = new SocketAsyncEventArgs();
            if (minBufferSize > 0)
            {
                var buffer = buffers.Rent(minBufferSize);
                e.SetBuffer(buffer, 0, buffer.Length);
            }
            return e;
        }
    }
}
