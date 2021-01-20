﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication.Test
{
    public class SendMsgTest
    {
        class Serve : SocketAsyncServe
        {
            protected override void AcceptSocket(SocketAsyncServe serve, Socket client)
            {
                new EchoLineReciver().BeginReceive(client);
            }
        }
        class EchoLineReciver : MsgPackReciver
        {
            int num;
            public static int LastResult;
            public override void OnRecv(object msg)
            {
                var sm = (SendNumber)msg;
                Debug.Assert(sm.Number == num);
                LastResult = sm.Number;
                num++;
            }
        }
        [MessagePackObject]
        public class SendNumber : SendMsgPack
        {
            [Key(1)]
            public int Number { get; set; }
        }
        public async Task Run()
        {
            MessageHeader.RegisterType(3, typeof(SendNumber));
            var serv = new Serve();
            serv.Start(new IPEndPoint(IPAddress.Any, 12345));

            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, 12345);
            var sender = new SocketAsyncSender(client.Client);

            int num = 0;
            for (int i = 0; i <= 100; i++)
            {
                for (int j = 0; j < 1000000; j++)
                {
                    sender.Enqueue(new SendNumber() { Number = num });
                    num++;
                }
                Console.WriteLine(EchoLineReciver.LastResult);
                await Task.Delay(500);
            }

            Console.ReadLine();
        }
    }
}