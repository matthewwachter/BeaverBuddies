using System;
using System.Net;
using System.Net.Sockets;

namespace TimberNet
{

    public class TCPListenerWrapper : ISocketListener
    {
        private readonly TcpListener listener;

        public TCPListenerWrapper(int port)
        {
            // Listen on IPv6 and IPv4
            listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        public ISocketStream AcceptClient()
        {
            return new TCPClientWrapper(listener.AcceptTcpClient());
        }

        public void Start()
        {
            listener.Start();
        }

        public void Stop()
        {
            try { listener.Stop(); }
            catch (Exception) { }
            try { listener.Server?.Dispose(); }
            catch (Exception) { }
        }
    }
}
