using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication.Test
{
    class BigData
    {
        class Serve : SocketAsyncServe
        {
            protected override void AcceptSocket(SocketAsyncServe serve, Socket client)
            {
                new RecvBlock { BufferSize = 1024 * 1024 }.BeginReceive(client);
            }
        }
        class RecvBlock : SocketAsyncReciver
        {
            public static long BytesCount;
            public override bool ContinueRecv(Socket socket, SocketAsyncEventArgs e)
            {
                BytesCount += e.BytesTransferred;
                return true;
            }
        }
        public class SendBlock : SendAble
        {
            public override void SetBuffer(SocketAsyncSender sender, byte[] buffer, ref int offset, ref bool flush, out bool completed)
            {
                offset = buffer.Length;
                completed = false;
                flush = true;
            }
        }
        public async Task Run()
        {
            var serv = new Serve();
            serv.Start(new IPEndPoint(IPAddress.Any, 12345));

            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, 12345);
            var sender = new SocketAsyncSender(client.Client) { BufferSize = 1024 * 8 };
            sender.Enqueue(new SendBlock());
            long last = 0;
            while (true)
            {
                Console.Write(RecvBlock.BytesCount);
                Console.Write("bytes\t+");
                Console.WriteLine(RecvBlock.BytesCount - last);
                last = RecvBlock.BytesCount;
                await Task.Delay(1000);
            }
        }
    }
}
