using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using mma.rest.server;
using mma.types;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace mma.rest.server2
{
    public class MMAChatHub : Hub
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


        public MMAChatHub(IChatResources _ChatResources)
        {
            this.chatRepo = _ChatResources;
        }

        public async override Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", "connected");
            await base.OnConnectedAsync();
        }

        public async Task RecieveMessageStream(RecieveMessageStreamRequest request)
        {
            var clientRequesterId = request?.RequestHeader?.ClientId;
            await SyncClientMessages(clientRequesterId);
        }

        public async Task SyncClientMessages(string ClientId)
        {
            var clientRequesterId = ClientId;
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
                        await Clients.Caller.SendAsync("RecieveMessage", m);
                        synced = true;
                    }

                    if (synced)
                        chatRepo.UpdateClientSyncDateTimeForChatRoom(clientActiveChatRoom, clientRequesterId);
                }
            }
        }



        public async Task SendMessage(SendMessageRequest request)
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
                        await Clients.GroupExcept(targetChatRoom.ChatRoomName, Context.ConnectionId).SendAsync("RecieveMessage", request?.ChatMessage);
                        chatRepo.addMessageToChatRoom(Guid.Parse(targetChatRoom.Id), request?.ChatMessage);
                    }
                }

                var reply = new SendMessageReply
                {
                    ACKMessage = new mmaAcknowledgeMessage
                    {
                        AckString = "Received",
                        MessageId = Guid.NewGuid().ToString(),
                        SentTime = DateTime.UtcNow,
                        Status = mmaRequestStatus.SUCCESS
                    },
                    ReplyHeader = BuildReplyHeader()
                };


            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SendCommand(SendCommandRequest request)
        {
            var rCommandType = request.CommandMessage.CommandType;
            var cParameters = request.CommandMessage.CommandParameters;
            var rClientId = request.RequestHeader.ClientId;
            var rClientName = request.RequestHeader.ClientName;

            var cResult = string.Empty;

            switch (rCommandType)
            {
                case mmaCommands.SHOW_ACTIVE_CHATROOMS: //Show Active ChatRooms
                    cResult = SendAvailableChatRooms(rClientId);
                    break;
                case mmaCommands.SHOW_MY_CHATROOMS: //Show My Active ChatRooms
                    cResult = SendMyAvailableChatRooms(rClientId);
                    break;
                case mmaCommands.LEAVE_ALL_CHATROOMS: //Leave All ChatRooms
                    cResult = LeaveAllChatRooms(rClientId);
                    break;
                case mmaCommands.LEAVE_CHATROOM: //Leave ChatRoom
                    cResult = LeaveChatRoom(cParameters, rClientId);
                    break;
                case mmaCommands.CREATE_NEW_CHATROOM: //Create New ChatRoom
                    cResult = CreateNewChatRoom(cParameters, rClientId);
                    break;
                case mmaCommands.JOIN_ACTIVE_CHATROOM: //Join Active ChatRoom
                    cResult = await JoinChatRoom(cParameters, rClientId);
                    break;
                case mmaCommands.GET_MENU: //Show MenuItems
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
                    SentTime = DateTime.UtcNow,
                    Status = mmaRequestStatus.SUCCESS,
                },
                ReplyHeader = BuildReplyHeader()
            };

            await Clients.Caller.SendAsync("ReceiveCommandResult", cReply);
        }


        public async Task SendHSMessage(SendHSMessageRequest request)
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
                    LastSyncDateTime = DateTime.UtcNow.AddMonths(-2)
                };
                chatRepo.addActiveClient(Guid.Parse(regClinet.ClientId), regClinet);
                ackMessage.Status = mmaRequestStatus.SUCCESS;
                ackMessage.AckString = MenuCommands;
                ackMessage.SentTime = DateTime.UtcNow;
                ackMessage.MessageId = Guid.NewGuid().ToString();

                sendHSMessageReply.HSReplyMessage = $"Welcome {regClinet.Name} to MMA Chat App you Joined the {chatRepo.GeneralChatRoomName}!";
                sendHSMessageReply.ACKMessage = ackMessage;
                sendHSMessageReply.ReplyHeader = BuildReplyHeader();

                await Clients.Caller.SendAsync("ReceiveHandShakeMessage", sendHSMessageReply);
                await Groups.AddToGroupAsync(Context.ConnectionId, chatRepo.GeneralChatRoomName);
                await SyncClientMessages(request.HSMessage.ClientId);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private string ReturnBadCommandError()
        {
            return "Unknown Error!";
        }

        private string ShowMenuItems()
        {
            return MenuCommands;
        }

        private async Task<string> JoinChatRoom(string cParameters, string rClientId)
        {
            if (!string.IsNullOrEmpty(cParameters))
            {
                var chatRoomId = cParameters;
                if (chatRepo.addClientToChatRoom(chatRoomId, rClientId))
                {
                    var chatRoomName = chatRepo.getChatRoomName(chatRoomId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomName);
                    await SyncClientMessages(rClientId);
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
                    Groups.AddToGroupAsync(Context.ConnectionId, cParameters);
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
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomName);
                    return $"You successfully left {chatRoomName} and transfered to the {chatRepo.GeneralChatRoomName}";
                }
            }

            return "Failed to Leave this ChatRoom or it is not your current active chatroom.";
        }

        private string LeaveAllChatRooms(string rClientId)
        {
            var crs = chatRepo.removeClientFromAllChatRooms(rClientId);
            if (crs?.Count > 0)
            {
                foreach (var item in crs)
                {
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, item);
                }
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

        private mmaReplyHeader BuildReplyHeader()
        {
            return new mmaReplyHeader
            {
                ServerId = chatRepo.ServerId,
                ServerName = chatRepo.ServerName,
                ServerTime = DateTime.UtcNow,
                GeneralChatRoomId = chatRepo.GeneralChatRoomID
            };
        }
    }
}