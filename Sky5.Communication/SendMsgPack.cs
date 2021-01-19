using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SendMsgPack : SendAble
    {
        protected virtual int BufferSize => 1024;
        public override void SetBuffer(SocketAsyncSender sender, byte[] buffer, ref int offset, ref bool flush, out bool completed)
        {
            if (buffer.Length - offset < BufferSize && buffer.Length > BufferSize)
            {
                flush = true;
                completed = false;
            }
            else
            {
                int begin = offset;
                offset += MessageHeader.ByteSize;
                MessagePackSerializer.Serialize(this.GetType(), sender, this);
                MessageHeader.Write(buffer, ref begin, offset - begin - MessageHeader.ByteSize);
                MessageHeader.Write(buffer, ref begin, MessageHeader.TypeToCode[this.GetType()]);
                completed = true;
            }
        }
    }
    public class MsgPackReciver : SocketAsyncReciver
    {
        int begin;
        int msgLen = -1;
        int typeCode = -1;
        public override bool ContinueRecv(Socket socket, SocketAsyncEventArgs e)
        {
            int len = e.Offset + e.BytesTransferred - begin;// 未处理的字节数，紧跟着begin
            bool Recv(ref int field)
            {
                if (field < 0)
                {
                    if (len < MessageHeader.NumSize)
                    {
                        EnsureBuffer(MessageHeader.NumSize);
                        return true;
                    }
                    else
                    {
                        field = MessageHeader.ReadInt(e.Buffer, ref begin);
                        len -= MessageHeader.NumSize;
                        return false;
                    }
                }
                return false;
            }
            void EnsureBuffer(int size)
            {
                if (e.Count - e.BytesTransferred < size)
                {
                    Buffer.BlockCopy(e.Buffer, begin, e.Buffer, 0, len);
                    begin = 0;
                    e.SetBuffer(len, e.Buffer.Length - len);
                }
                else
                    e.SetBufferAppend();
            }
            while (true)
            {
                if (Recv(ref msgLen)) return true;
                if (Recv(ref typeCode)) return true;
                if (len < msgLen)
                {
                    EnsureBuffer(msgLen);
                    return true;
                }
                else
                {
                    var bytes = new ReadOnlyMemory<byte>(e.Buffer, begin, msgLen);
                    var msg = MessagePackSerializer.Deserialize(MessageHeader.CodeToType[typeCode], bytes);
                    OnRecv(msg);
                    len -= msgLen;
                    begin += msgLen;
                    typeCode = -1;
                    msgLen = -1;
                }
            }
        }
        public virtual void OnRecv(object msg) { }
    }
}
