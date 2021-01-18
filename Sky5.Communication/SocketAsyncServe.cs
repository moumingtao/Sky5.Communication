using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    public class SocketAsyncServe
    {
        public int MaxClients = 10;
        public readonly List<Socket> ConnectedSockets = new List<Socket>();
        Timer stateCheck;
        Socket socketListen;
        SocketAsyncEventArgs argsAccept;
        public void Start(IPEndPoint localEndPoint)
        {
            socketListen = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketListen.Bind(localEndPoint);
            socketListen.Listen(100);
            argsAccept = new SocketAsyncEventArgs();
            argsAccept.Completed += AcceptCompleted;
            StartAccept();
        }
        bool StartAccept()
        {
            if (ConnectedSockets.Count >= MaxClients) return false;
            if (!socketListen.AcceptAsync(argsAccept))
                    AcceptCompleted(socketListen, argsAccept);
            return true;
        }
        void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            lock (ConnectedSockets)
            {
                ConnectedSockets.Add(e.AcceptSocket);
                if (ConnectedSockets.Count == 1)
                {
                    stateCheck = new Timer(CheckStatus, null, 1000, Timeout.Infinite);
                }
            }
            AcceptSocket(this, e.AcceptSocket);
            e.AcceptSocket = null;
            StartAccept();
        }

        private void CheckStatus(object state)
        {
            lock (ConnectedSockets)
            {
                int newIndex = 0;
                for (int oldIndex = 0; oldIndex < ConnectedSockets.Count; oldIndex++)
                {
                    if (ConnectedSockets[oldIndex].Connected)
                    {
                        if (oldIndex != newIndex)
                            ConnectedSockets[newIndex] = ConnectedSockets[oldIndex];
                        newIndex++;
                    }
                    else
                        ConnectedSockets[oldIndex].Close();
                }
                if (newIndex < ConnectedSockets.Count)
                {
                    var isMax = ConnectedSockets.Count == MaxClients;
                    ConnectedSockets.RemoveRange(newIndex, ConnectedSockets.Count - newIndex);
                    if (isMax) StartAccept();
                    if (ConnectedSockets.Count == 0) return;
                }
            }
            stateCheck.Change(1000, Timeout.Infinite);
        }

        protected virtual void AcceptSocket(SocketAsyncServe serve, Socket client) { }
    }
}
