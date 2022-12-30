using System;
namespace mma.model
{
    [Serializable]
    public class mmaMessage
    {

    }


    [Serializable]
    public class Message : mmaMessage
    {
        public Guid MessageId { get; set; }
        public DateTime SentTime { get; set; }
        public DateTime ReceivedTime { get; set; }
        public string? MessageText { get; set; }
        public string? UserName { get; set; }
        public Guid? ClientID { get; set; }
        public Guid? ChatRoomID { get; set; }
        public MessageTypeEnum? messageTypeEnum { get; set; }
    }


    public enum MessageTypeEnum
    {
        MessageText,
        MessageEvent,
        MessageCommand,
        MessageInformation
    }

    [Serializable]
    public class HandShakeMessage : mmaMessage
    {
        public Guid ClientId { get; set; }
        public string? MenuCommands { get; set; }
        public string? WelcomeMessage { get; set; }
    }



    [Serializable]
    public class CommandMessage : mmaMessage
    {
        public string Command { get; set; }
        public Guid ClientId { get; set; }
    }
}

