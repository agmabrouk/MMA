using System;
using System.Net.Sockets;
using System.Text;

namespace mma.model
{
    [Serializable]
    public class Client
    {
        public Guid Id { get; set; }
        public string? ipAddress { get; set; }
        public string? Username { get; set; }
        public Guid? CurrentActiveChatRoom { get; set; }
        public Dictionary<Guid, DateTime>? ActiveChatRoomWithLastSyncDateTime { get; set; }
        public TcpClient? ClientConnection { get; set; }
        public NetworkStream? ClientStream { get; set; }
        public byte[]? ClientBuffer { get; set; }
        public StringBuilder? ClientData { get; set; }
        public EventWaitHandle? ClientHandle { get; set; }
    }
}

