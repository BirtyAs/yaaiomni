﻿using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Terraria.Net;
using Terraria.Net.Sockets;
using TerrariaApi.Server;
using TShockAPI.Sockets;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Socket_OnCreate(object? sender, OTAPI.Hooks.Netplay.CreateTcpListenerEventArgs args)
    {
        switch (this.config.Socket)
        {
            case Config.SocketType.Vanilla:
                args.Result = new Terraria.Net.Sockets.TcpSocket();
                return;
            case Config.SocketType.TShock:
                args.Result = new LinuxTcpSocket();
                return;
            case Config.SocketType.AsIs:
                return;
            case Config.SocketType.Unset:
                args.Result = null;
                return;
            case Config.SocketType.HackyBlocked:
                args.Result = new Socket.HackyBlockedSocket();
                return;
            case Config.SocketType.HackyAsync:
                args.Result = new Socket.HackyAsyncSocket();
                return;
            case Config.SocketType.AnotherAsyncSocket:
            case Config.SocketType.Preset:
                args.Result = new Socket.AnotherAsyncSocket();
                return;
        }
    }
}

internal static class Socket
{
    public abstract class SelfSocket : ISocket
    {
        public TcpClient _connection;

        public TcpListener? _listener;

        public SocketConnectionAccepted? _listenerCallback;

        public RemoteAddress? _remoteAddress;

        public bool _isListening;

        public SelfSocket()
        {
            this._connection = new TcpClient();
            this._connection.NoDelay = true;
        }

        public SelfSocket(TcpClient tcpClient)
        {
            this._connection = tcpClient;
            this._connection.NoDelay = true;
            var iPEndPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint!;
            this._remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
        }

        void ISocket.Close()
        {
            this._remoteAddress = null;
            this._connection!.Close();
        }

        bool ISocket.IsConnected()
        {
            return this._connection != null && this._connection.Client != null && this._connection.Connected;
        }

        void ISocket.Connect(RemoteAddress address)
        {
            var tcpAddress = (TcpAddress) address;
            this._connection!.Connect(tcpAddress.Address, tcpAddress.Port);
            this._remoteAddress = address;
        }

        void ISocket.SendQueuedPackets()
        {
        }

        bool ISocket.IsDataAvailable()
        {
            return this._connection!.GetStream().DataAvailable;
        }

        RemoteAddress ISocket.GetRemoteAddress()
        {
            return this._remoteAddress!;
        }

        void ISocket.StopListening()
        {
            this._isListening = false;
        }

        bool ISocket.StartListening(SocketConnectionAccepted callback)
        {
            var address = IPAddress.Any;
            if (Terraria.Program.LaunchParameters.TryGetValue("-ip", out var value) && !IPAddress.TryParse(value, out address))
            {
                address = IPAddress.Any;
            }

            this._isListening = true;
            this._listenerCallback = callback;
            this._listener ??= new TcpListener(address, Terraria.Netplay.ListenPort);

            try
            {
                this._listener.Start();
            }
            catch (Exception)
            {
                return false;
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ListenLoop));
            return true;
        }

        internal void ListenLoop(object? _)
        {
            while (this._isListening && !Terraria.Netplay.Disconnect)
            {
                try
                {
                    var socket = this.New(this._listener!.AcceptTcpClient());
                    Console.WriteLine(Terraria.Localization.Language.GetTextValue("Net.ClientConnecting", socket.GetRemoteAddress()));
                    this._listenerCallback!(socket);
                }
                catch (Exception)
                {
                }
            }

            this._listener!.Stop();
            Terraria.Netplay.IsListening = false;
        }

        internal abstract ISocket New(TcpClient client);
        public abstract void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object? state = null);
        public abstract void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object? state = null);
    }

    public class HackyBlockedSocket : SelfSocket
    {
        public HackyBlockedSocket() : base()
        {
        }

        public HackyBlockedSocket(TcpClient tcpClient) : base(tcpClient)
        {
        }

        internal override ISocket New(TcpClient client)
        {
            return new HackyBlockedSocket(client);
        }

        public override void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object? state = null)
        {
            callback(state);
            this._connection!.GetStream().Write(data, offset, size);
        }

        public override void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object? state = null)
        {
            var read = this._connection.GetStream().Read(data, offset, size);
            callback(state, read);
        }
    }

    public class HackyAsyncSocket : SelfSocket
    {
        public HackyAsyncSocket() : base()
        {
        }

        public HackyAsyncSocket(TcpClient tcpClient) : base(tcpClient)
        {
        }

        internal override ISocket New(TcpClient client)
        {
            return new HackyAsyncSocket(client);
        }

        public override void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object? state = null)
        {
            callback(state);
            try
            {
                this._connection.GetStream().WriteAsync(data, offset, size);
            }
            catch
            {
            }
        }

        public override void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object? state = null)
        {
            try
            {
                this._connection.GetStream().ReadAsync(data, offset, size).ContinueWith(task =>
                {
                    callback(state, task.Result);
                });
            }
            catch
            {
            }
        }
    }

    public class AnotherAsyncSocket : SelfSocket
    {
        public AnotherAsyncSocket() : base()
        {
        }

        public AnotherAsyncSocket(TcpClient tcpClient) : base(tcpClient)
        {
        }

        internal override ISocket New(TcpClient client)
        {
            return new AnotherAsyncSocket(client);
        }

        public override async void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object? state = null)
        {
            try
            {
                // Netplay.KickClient use a static shared buffer, might be better to copy it before use
                // ArrayPool sounds smarter than vanilla's Net.LegacyNetBufferPool
                var buffer = ArrayPool<byte>.Shared.Rent(size);
                Buffer.BlockCopy(data, offset, buffer, 0, size);
                await this._connection.GetStream().WriteAsync(buffer.AsMemory(0, size));
                callback(state);
                ArrayPool<byte>.Shared.Return(buffer);
            }
            catch
            {
            }
        }

        public override async void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object? state = null)
        {
            try
            {
                var read = await this._connection.GetStream().ReadAsync(data, offset, size);
                callback(state, read);
            }
            catch
            {
            }
        }
    }
}
