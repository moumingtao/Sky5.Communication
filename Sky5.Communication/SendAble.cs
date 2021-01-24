using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public abstract class SendAble
    {
        volatile internal SendAble Next;
        public abstract void SetBuffer(SocketAsyncSender sender, byte[] buffer, ref int offset, ref bool flush, out bool completed);
    }
}
