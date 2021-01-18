using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    class SocketAsyncEventArgsPool
    {
        class SocketAsyncEventArgsLink : SocketAsyncEventArgs
        {
            public int CreationIndex;
            public SocketAsyncEventArgsLink NextFree;
            public SocketAsyncEventArgsPool Pool;
        }

        SocketAsyncEventArgsLink FreeLink;// 空闲的SocketAsyncEventArgs
        public void Free(SocketAsyncEventArgs item)
        {
            if (item is not SocketAsyncEventArgsLink link)
                throw new ArgumentException("传入的对象不是SocketAsyncEventArgsLink类型");
            if (link.Pool != this)
                throw new ArgumentException("该对象不是从当前池中获取的");
            lock (this)
            {
                ref SocketAsyncEventArgsLink curr = ref FreeLink;
            INSERT:
                if (curr == null) curr = link;
                else if (curr.CreationIndex < link.CreationIndex)
                {
                    link.NextFree = curr;
                    curr = link;
                }
                else if (curr == link) { }
                else
                {
                    curr = ref curr.NextFree;
                    goto INSERT;
                }
            }
        }
        int creationIndex;
        public int CreationIndex => creationIndex;
        public SocketAsyncEventArgs GetOne()
        {
            lock (this)
            {
                if (FreeLink == null)
                    return new SocketAsyncEventArgsLink { CreationIndex = creationIndex++, Pool = this };
                else
                {
                    var curr = FreeLink;
                    FreeLink = FreeLink.NextFree;
                    curr.NextFree = null;
                    return curr;
                }
            }
        }
    }
}
