using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    static class Util
    {
        public static void SetBufferAppend(this SocketAsyncEventArgs e)
            => e.SetBuffer(e.Offset + e.BytesTransferred, e.Count - e.BytesTransferred);
    }
}
