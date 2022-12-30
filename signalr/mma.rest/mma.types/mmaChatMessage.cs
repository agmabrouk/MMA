namespace mma.types
{
    public class SendNotificationRequest
    {
        public mmaRequestHeader? RequestHeader { get; set; }
        public mmaNotifcationMessage? NotificationMessage { get; set; }
    }

    public class SendNotificationReply
    {
        public mmaReplyHeader? ReplyHeader { get; set; }
        public mmaAcknowledgeMessage? ACKMessage { get; set; }
    }

    public class RecieveMessageStreamRequest
    {
        public mmaRequestHeader? RequestHeader { get; set; }
        public DateTime LastSyncDateTime { get; set; }
    }

    public class RecieveMessageStreamReply
    {
        public mmaReplyHeader? ReplyHeader { get; set; }
        public mmaChatMessage? Message { get; set; }
    }

    public class SendCommandRequest
    {
        public mmaRequestHeader? RequestHeader { get; set; }
        public mmaCommandMessage? CommandMessage { get; set; }
    }

    public class SendMessageReply
    {
        public mmaReplyHeader? ReplyHeader { get; set; }
        public mmaAcknowledgeMessage? ACKMessage { get; set; }
    }

    public class SendMessageRequest
    {
        public mmaRequestHeader? RequestHeader { get; set; }
        public mmaChatMessage? ChatMessage { get; set; }
    }
    public class SendHSMessageRequest
    {
        public mmaRequestHeader? RequestHeader { get; set; }
        public mmaHandShakeMessage? HSMessage { get; set; }
    }

    public class SendHSMessageReply
    {
        public mmaReplyHeader? ReplyHeader { get; set; }
        public mmaAcknowledgeMessage? ACKMessage { get; set; }
        public string? HSReplyMessage { get; set; }
    }

    public class SendCommandReply
    {
        public mmaReplyHeader? ReplyHeader { get; set; }
        public mmaAcknowledgeMessage? ACKMessage { get; set; }
    }

    public class mmaReplyHeader
    {
        public string? ServerName { get; set; }
        public string? ServerId { get; set; }
        public DateTime ServerTime { get; set; }
        public string? GeneralChatRoomId { get; set; }
    }

    public class mmaRequestHeader
    {
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime SentTime { get; set; }
    }

    public class mmaChatRoom
    {
        public string? Id { get; set; }
        public List<mmaChatMessage>? ChatRoomMessages { get; set; }
        public string? ChatRoomName { get; set; }
        public DateTime CreationTime { get; set; }
        public bool isActive { get; set; }
        public List<mmaClient>? ChatRoomClients { get; set; }
    }

    public class mmaClient
    {
        public string? Name { get; set; }
        public string? ClientId { get; set; }
        public string? ActiveChatRoom { get; set; }
        public DateTime LastSyncDateTime { get; set; }
    }

    public class mmaChatMessage
    {
        public string? MessageId { get; set; }
        public DateTime SentTime { get; set; }
        public DateTime ReceivedTime { get; set; }
        public string? MessageText { get; set; }
        public string? UserName { get; set; }
        public string? ClientId { get; set; }
        public string? ChatRoomId { get; set; }
    }

    public class mmaHandShakeMessage
    {
        public string? MessageId { get; set; }
        public DateTime SentTime { get; set; }
        public string? MessageText { get; set; }
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }

    }
    public class mmaAcknowledgeMessage
    {
        public string? MessageId { get; set; }
        public DateTime SentTime { get; set; }
        public mmaRequestStatus Status { get; set; }
        public string? AckString { get; set; }
    }

    public enum mmaRequestStatus
    {
        SUCCESS = 0,
        FAILED = 1,
        PENDING = 2,
        CANCELED = 3,
    }

    public enum mmaSystemMessageTypes
    {
        NotifcationMessage_Type = 0,
        HandShakeMessage_Type = 1,
        ChatMessage_Type = 2,
        AcknowledgeMessage_Type = 3,
    }

    public class mmaCommandMessage
    {
        public mmaCommands CommandType { get; set; }
        public string? CommandParameters { get; set; }
        public DateTime SentTime { get; set; }
    }

    public enum mmaCommands
    {
        GET_MENU = 0,
        SHOW_ACTIVE_CHATROOMS = 1,
        SHOW_MY_CHATROOMS = 2,
        LEAVE_ALL_CHATROOMS = 3,
        LEAVE_CHATROOM = 4,
        CREATE_NEW_CHATROOM = 5,
        JOIN_ACTIVE_CHATROOM = 6
    }

    public class mmaNotifcationMessage
    {
        public mmaNotifications NotificationType { get; set; }
        public string? NotificationText { get; set; }
        public DateTime SentTime { get; set; }
    }

    public enum mmaNotifications
    {
        CLIENT_NOTIFICATION = 0,
        GENERAL_NOTIFICATION = 1
    }
}