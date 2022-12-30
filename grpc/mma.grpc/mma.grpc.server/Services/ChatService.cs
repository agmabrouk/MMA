using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using mma.grpc.server;
using mma.grpc.server.helper;

public class ChatService : mmaChatService.mmaChatServiceBase
{
    private static ILogger _log = new Logger();

    private IChatResources chatRepo;

    private const string CommandSplittor = "!";
    private const string SystemMessage = "System Message";

    private static readonly string MenuCommands = $"Menu Commands:{Environment.NewLine}" +
    $"CD!           ==> List Available ChatRooms {Environment.NewLine}" +
    $"MCD!          ==> List My Available ChatRooms {Environment.NewLine}" +
    $"SM!           ==> Show Menu Commands {Environment.NewLine}" +
    $"CLA!          ==> Leave All ChatRooms {Environment.NewLine}" +
    $"CL![RoomID]   ==> Leave ChatRoom {Environment.NewLine}" +
    $"CJ![RoomID]   ==> Join ChatRoom {Environment.NewLine}" +
    $"CC![RoomName] ==> Create New ChatRoom {Environment.NewLine}";


    public ChatService(IChatResources _ChatResources)
    {
        this.chatRepo = _ChatResources;
    }


    public override Task<SendHSMessageReply> SendHSMessage(SendHSMessageRequest request, ServerCallContext context)
    {
        try
        {
            mmaAcknowledgeMessage ackMessage = new mmaAcknowledgeMessage();
            SendHSMessageReply sendHSMessageReply = new SendHSMessageReply();
            mmaReplyHeader mmaReplyHeader = new mmaReplyHeader();
            mmaHandShakeMessage message = new mmaHandShakeMessage();

            mmaClient regClinet = new mmaClient
            {
                Name = request.HSMessage.ClientName,
                ClientId = request.HSMessage.ClientId,
                ActiveChatRoom = chatRepo.GeneralChatRoomID,
                LastSyncDateTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(-2))
            };
            chatRepo.addActiveClient(Guid.Parse(regClinet.ClientId), regClinet);
            ackMessage.Status = mmaRequestStatus.Success;
            ackMessage.AckString = MenuCommands;
            ackMessage.SentTime = Timestamp.FromDateTime(DateTime.UtcNow);
            ackMessage.MessageId = Guid.NewGuid().ToString();

            sendHSMessageReply.HSReplyMessage = $"Welcome {regClinet.Name} to MMA Chat App you Joined the {chatRepo.GeneralChatRoomName}!";
            sendHSMessageReply.ACKMessage = ackMessage;
            sendHSMessageReply.ReplyHeader = mmaReplyHeader;

            return Task.FromResult(sendHSMessageReply);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public override Task<SendCommandReply> SendCommand(SendCommandRequest request, ServerCallContext context)
    {
        var rCommandType = request.CommandMessage.CommandType;
        var cParameters = request.CommandMessage.CommandParameters;
        var rClientId = request.RequestHeader.ClientId;
        var rClientName = request.RequestHeader.ClientName;

        var cResult = string.Empty;

        switch (rCommandType)
        {
            case mmaCommands.ShowActiveChatrooms: //Show Active ChatRooms
                cResult = SendAvailableChatRooms(rClientId);
                break;
            case mmaCommands.ShowMyChatrooms: //Show My Active ChatRooms
                cResult = SendMyAvailableChatRooms(rClientId);
                break;
            case mmaCommands.LeaveAllChatrooms: //Leave All ChatRooms
                cResult = LeaveAllChatRooms(rClientId);
                break;
            case mmaCommands.LeaveChatroom: //Leave ChatRoom
                cResult = LeaveChatRoom(cParameters, rClientId);
                break;
            case mmaCommands.CreateNewChatroom: //Create New ChatRoom
                cResult = CreateNewChatRoom(cParameters, rClientId);
                break;
            case mmaCommands.JoinActiveChatroom: //Join Active ChatRoom
                cResult = JoinChatRoom(cParameters, rClientId);
                break;
            case mmaCommands.GetMenu: //Show MenuItems
                cResult = ShowMenuItems();
                break;
            default: //Not agreed command
                cResult = ReturnBadCommandError();
                break;
        }


        var cReply = new SendCommandReply
        {
            ACKMessage = new mmaAcknowledgeMessage
            {
                AckString = cResult,
                MessageId = Guid.NewGuid().ToString(),
                SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                Status = mmaRequestStatus.Success,
            },
            ReplyHeader = BuildReplyHeader()
        };

        return Task.FromResult(cReply);
    }

    private string ReturnBadCommandError()
    {
        return "Unknown Error!";
    }

    private string ShowMenuItems()
    {
        return MenuCommands;
    }

    private string JoinChatRoom(string cParameters, string rClientId)
    {
        if (!string.IsNullOrEmpty(cParameters))
        {
            var chatRoomId = cParameters;
            if (chatRepo.addClientToChatRoom(chatRoomId, rClientId))
            {
                var chatRoomName = chatRepo.getChatRoomName(chatRoomId);
                return $"Welcome to {chatRoomName} ChatRoom";
            }
        }
        return "Failed to Join this chatroom";
    }

    private string CreateNewChatRoom(string cParameters, string rClientId)
    {
        if (!string.IsNullOrEmpty(cParameters))
        {
            if (chatRepo.createNewChatRoom(cParameters, rClientId))
            {
                return $"Chat Room has been created Successfully and your actice chatroom now is {cParameters}";
            }
        }
        return "Failed to create this chatroom";
    }

    private string LeaveChatRoom(string cParameters, string rClientId)
    {
        if (!string.IsNullOrEmpty(cParameters))
        {
            if (chatRepo.removeClientFromChatRoom(cParameters, rClientId))
            {
                var chatRoomName = chatRepo.getChatRoomName(cParameters);
                return $"You successfully left {chatRoomName} and transfered to the {chatRepo.GeneralChatRoomName}";
            }
        }

        return "Failed to Leave this ChatRoom or it is not your current active chatroom.";
    }

    private string LeaveAllChatRooms(string rClientId)
    {
        if (chatRepo.removeClientFromAllChatRooms(rClientId))
        {
            return $"You successfully left all the chatrooms and you are now on the default chat room {chatRepo.getChatRoomName}";
        }

        return "Failed to execute this command";
    }

    private string SendMyAvailableChatRooms(string rClientId)
    {
        return chatRepo.getClientChatRooms(rClientId);
    }

    private string SendAvailableChatRooms(string rClientId)
    {
        return chatRepo.getAllActiveChatRooms(rClientId);
    }

    public override Task<SendMessageReply> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        try
        {
            var clientRequesterId = request?.RequestHeader?.ClientId;
            var clientActiveChatRoom = chatRepo.getActiveClient(Guid.Parse(clientRequesterId ?? string.Empty))?.ActiveChatRoom;
            if (clientActiveChatRoom != null)
            {
                var targetChatRoom = chatRepo.getActiveChatRoom(Guid.Parse(clientActiveChatRoom ?? string.Empty));
                if (targetChatRoom != null)
                {
                    chatRepo.addMessageToChatRoom(Guid.Parse(targetChatRoom.Id), request?.ChatMessage);
                }
            }

            return Task.FromResult(new SendMessageReply
            {
                ACKMessage = new mmaAcknowledgeMessage
                {
                    AckString = "Received",
                    MessageId = Guid.NewGuid().ToString(),
                    SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    Status = mmaRequestStatus.Success
                },
                ReplyHeader = BuildReplyHeader()
            });
        }
        catch (Exception)
        {
            return Task.FromResult(new SendMessageReply
            {
                ACKMessage = new mmaAcknowledgeMessage
                {
                    AckString = "Failed: Error saving your message to the server",
                    MessageId = Guid.NewGuid().ToString(),
                    SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    Status = mmaRequestStatus.Failed
                },
                ReplyHeader = BuildReplyHeader()
            });

            throw;
        }
    }


    public override Task RecieveMessageStream(RecieveMessageStreamRequest request, IServerStreamWriter<RecieveMessageStreamReply> responseStream, ServerCallContext context)
    {
        var clientRequesterId = request?.RequestHeader?.ClientId;
        var lastSyncDate = request?.LastSyncDateTime;
        var client = chatRepo.getActiveClient(Guid.Parse(clientRequesterId ?? string.Empty));
        var clientActiveChatRoom = client?.ActiveChatRoom;

        if (clientActiveChatRoom != null)
        {
            var targetChatRoom = chatRepo.getActiveChatRoom(Guid.Parse(clientActiveChatRoom ?? string.Empty));
            var clientLastSyncForThisRoom = targetChatRoom?.ChatRoomClients?.Where(c => c.ClientId == clientRequesterId).FirstOrDefault()?.LastSyncDateTime;
            if (targetChatRoom != null)
            {
                bool synced = false;
                foreach (var m in targetChatRoom.ChatRoomMessages.OrderBy(x => x.SentTime).Where(x => x.SentTime >= clientLastSyncForThisRoom && x.ClientId != clientRequesterId))
                {
                    responseStream.WriteAsync(new RecieveMessageStreamReply
                    {
                        Message = m,
                        ReplyHeader = BuildReplyHeader()
                    });
                    synced = true;
                }

                if (synced)
                    chatRepo.UpdateClientSyncDateTimeForChatRoom(clientActiveChatRoom, clientRequesterId);
            }
        }


        return Task.CompletedTask;
    }

    private mmaReplyHeader BuildReplyHeader()
    {
        return new mmaReplyHeader
        {
            ServerId = chatRepo.ServerId,
            ServerName = chatRepo.ServerName,
            ServerTime = Timestamp.FromDateTime(DateTime.UtcNow),
            GeneralChatRoomId = chatRepo.GeneralChatRoomID
        };
    }

    public override Task StreamNotification(IAsyncStreamReader<SendNotificationRequest> requestStream, IServerStreamWriter<SendNotificationReply> responseStream, ServerCallContext context)
    {
        return base.StreamNotification(requestStream, responseStream, context);
    }
}