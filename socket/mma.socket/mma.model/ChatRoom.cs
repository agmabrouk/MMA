namespace mma.model;
public class ChatRoom
{
    public Guid id { set; get; }
    public List<Message>? MessagesList { get; set; }
    public string? RoomName { get; set; }
    public DateTime CreationDate { get; set; }
    public List<Guid>? ActiveClientsIds { get; set; }
}

