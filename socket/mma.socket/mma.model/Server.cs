using System;
namespace mma.model
{
    [Serializable]
    public class Server
    {
        public Guid id { get; set; }
        public string? ServerName { get; set; }
        public Dictionary<int, List<ChatRoom>>? ActiveChatRooms { get; set; }
        public DateTime StartTime { get; set; }
    }
}

