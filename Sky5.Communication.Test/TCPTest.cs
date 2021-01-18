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
            int num;
            public static string LastResult;
            protected override bool ContinueLine(EndPoint remote, string content)
            {
                Debug.Assert(int.Parse(content) == num);
                LastResult = content;
                num++;
                return true;
            }
        }
        public async Task Run()
        {
            var serv = new Serve();
            var client = new TcpClient();
            serv.Start(new IPEndPoint(IPAddress.Any, 12345));
            await client.ConnectAsync(IPAddress.Loopback, 12345);
            var sender = new SocketAsyncSender(client.Client);

            int num = 0;
            for (int i = 0; i <= 100; i++)
            {
                for (int j = 0; j < 1000000; j++)
                {
                    sender.Send(new SendString(num.ToString()));
                    sender.Send(new SendString("\r\n"));
                    num++;
                }
                Console.WriteLine(EchoLineReciver.LastResult);
                await Task.Delay(1000);
            }

            Console.ReadLine();
        }
    }
}
