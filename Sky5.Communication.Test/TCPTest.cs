using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication.Test
{
    class TCPTest
    {
        class Serve : SocketAsyncServe
        {
            protected override void AcceptSocket(SocketAsyncServe serve, Socket client)
            {
                new EchoLineReciver().Begin(client);
            }
        }
        class EchoLineReciver : SplitStringReciver
        {
            public const int EndNum = 10000000;
            int num;
            protected override bool ContinueLine(EndPoint remote, string content)
            {
                Debug.Assert(long.Parse(content) >= num);
                num++;
                if (num % 100000 == 0) Console.WriteLine(num);
                return num < EndNum;
            }
        }
        public async Task Run()
        {
            var serv = new Serve();
            var client = new TcpClient();
            serv.Start(new IPEndPoint(IPAddress.Any, 12345));
            await client.ConnectAsync(IPAddress.Loopback, 12345);
            var sender = new SocketAsyncSender(client.Client);

            for (int i = 0; i < EchoLineReciver.EndNum; i++)
            {
                sender.Send(new SendString(i.ToString()));
                sender.Send(new SendString("\r\n"));
            }

            Console.ReadLine();
        }
    }
}
