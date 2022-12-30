using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using mma.common;
using mma.model;

namespace mma.server
{
    public class mmaServer
    {
        private static Logger _log = new Logger();
        static readonly object _lock = new object();

        private static Guid GeneralChatRoomID = Guid.NewGuid();
        public static ConcurrentDictionary<Guid, Client> ActiveClients = new ConcurrentDictionary<Guid, Client>();
        public static ConcurrentDictionary<Guid, ChatRoom> ActiveChatRooms = new ConcurrentDictionary<Guid, ChatRoom>();
        public static ConcurrentDictionary<Guid, Message> SystemMessages = new ConcurrentDictionary<Guid, Message>();
        public static ConcurrentDictionary<string, Guid> ChatRoomsNamesDic = new ConcurrentDictionary<string, Guid>();

        public const int streamSize = 1024;
        public const int serverPort = 6565;
        public const string CommandSplittor = "!";
        private static readonly string MenuCommands = $"Menu Commands:{Environment.NewLine}" +
        $"CD! ==>List Available ChatRooms {Environment.NewLine}" +
        $"MCD! ==>List My Available ChatRooms {Environment.NewLine}" +
        $"CJ![RoomID] ==> Join ChatRoom {Environment.NewLine}" +
        $"CLA! ==> Leave All ChatRooms {Environment.NewLine}" +
        $"CL![RoomID] ==> Leave ChatRoom {Environment.NewLine}" +
        $"SM! ==> Show Menu Commands {Environment.NewLine}" +
        $"CC![RoomName] ==> Create New ChatRoom {Environment.NewLine}";
        private const string SystemMessage = "System Message";

        public mmaServer()
        {
            try
            {
                //Start Server
                TcpListener ServerSocket = new TcpListener(serverPort);
                ServerSocket.Start();
                LogInfo($"Server Started At:{DateTime.Now} with General ChatRoom ID: {GeneralChatRoomID.ToString()}");

                //Create General Chat Room
                ChatRoom generalChatRoom = new ChatRoom() { ActiveClientsIds = new List<Guid>(), CreationDate = DateTime.UtcNow, id = GeneralChatRoomID, MessagesList = new List<Message>(), RoomName = "General ChatRoom" };

                lock (_lock) ActiveChatRooms.TryAdd(generalChatRoom.id, generalChatRoom);
                lock (_lock) ChatRoomsNamesDic.TryAdd(generalChatRoom.RoomName.Trim().ToLower(), generalChatRoom.id);

                while (true)
                {
                    //Register New Clients
                    TcpClient client = ServerSocket.AcceptTcpClient();
                    Client joinedClient = new Client()
                    {
                        Id = Guid.NewGuid(),
                        ClientConnection = client,
                        CurrentActiveChatRoom = generalChatRoom.id,
                        ActiveChatRoomWithLastSyncDateTime = new Dictionary<Guid, DateTime>()
                        {
                            {GeneralChatRoomID, DateTime.Now},
                        }
                    };

                    lock (_lock) ActiveClients.TryAdd(joinedClient.Id, joinedClient); //Add to Active Clients
                    lock (_lock) ActiveChatRooms[GeneralChatRoomID]?.ActiveClientsIds?.Add(joinedClient.Id);  //Add to General Chat Room

                    LogInfo($"Client joined to the server from {((IPEndPoint?)client?.Client?.RemoteEndPoint)?.Address}");
                    new Thread(() => handle_clients(joinedClient)).Start();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        public static void handle_clients(object? obj)
        {
            if (obj is null)
                return;

            Client? nc = (Client)obj;
            TcpClient? client = null;

            try
            {
                //send handshake message to client
                lock (_lock) client = ActiveClients[nc.Id].ClientConnection ?? null;
                SendHandShakeMessageToClient(nc);
                //Read messages from CLient
                while (true)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int byte_count = stream.Read(buffer, 0, buffer.Length);

                    if (byte_count == 0)
                    {
                        break;
                    }

                    string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                    LogMessage(data);
                    ParseUserMessage(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (client != null)
                {
                    client.Client?.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                throw;
            }
        }

        private static void ParseUserMessage(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;
            try
            {
                Message? m = JsonSerializer.Deserialize<Message>(data) ?? null;
                if (m is null)
                {
                    return;
                }

                var messageSplitted = m?.MessageText?.Split(CommandSplittor);
                if (messageSplitted != null && messageSplitted.Length > 0)
                {
                    var command = messageSplitted[0].ToUpper();
                    switch (command)
                    {
                        case "CD": //Show Active ChatRooms
                            SendAvailableChatRooms(m);
                            break;
                        case "MCD": //Show My Active ChatRooms
                            SendMyAvailableChatRooms(m);
                            break;
                        case "CLA": //Leave All ChatRooms
                            LeaveAllChatRooms(m);
                            break;
                        case "CL": //Leave ChatRoom
                            if (!string.IsNullOrEmpty(messageSplitted[1].Trim()))
                                LeaveChatRoom(messageSplitted[1].Trim().ToUpper(), m);
                            break;
                        case "CC": //Create New ChatRoom
                            if (!string.IsNullOrEmpty(messageSplitted[1].Trim()))
                                CreateNewChatRoom(messageSplitted[1].Trim().ToUpper(), m);
                            break;
                        case "CJ": //Join Active ChatRoom
                            if (!string.IsNullOrEmpty(messageSplitted[1].Trim().ToUpper()))
                                JoinChatRoom(messageSplitted[1].Trim().ToUpper(), m);
                            break;
                        case "SM": //Show MenuItems
                            ShowMenuItems(m);
                            break;
                        default://Not agreed command
                            Broadcast(data);
                            break;
                    }
                }
                else
                    Broadcast(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void ShowMenuItems(Message? m)
        {
            try
            {
                ActiveClients.TryGetValue(m.ClientID ?? Guid.NewGuid(), out var client);
                if (client != null)
                {
                    m.MessageText = MenuCommands;
                    m.UserName = SystemMessage;
                    m.ChatRoomID = GeneralChatRoomID;
                    SendMessageToClient(client, m);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static void JoinChatRoom(string chatroomId, Message? m)
        {
            try
            {
                if (chatroomId == null) return;
                if (m == null) return;

                m.UserName = SystemMessage;
                m.ChatRoomID = GeneralChatRoomID;

                ActiveClients.TryGetValue(m.ClientID ?? Guid.NewGuid(), out var client);
                if (client is null)
                {
                    LogError($"Invalid Client");
                    return;
                }

                if (string.IsNullOrEmpty(chatroomId))
                {
                    LogError($"Invalid Chat Room ID {chatroomId} from ClientID {client.Username}");
                    m.MessageText = $"Invalid Chat Room ID {chatroomId}";
                    SendMessageToClient(client, m);
                    return;
                }

                ActiveChatRooms.TryGetValue(Guid.Parse(chatroomId), out var chatRoom);
                if (chatRoom is null)
                {
                    LogError($"Invalid Chat Room ID {chatroomId} from ClientID {client.Username}");
                    m.MessageText = $"Invalid Chat Room ID {chatroomId} or not exist";
                    SendMessageToClient(client, m);
                    return;
                }

                if (chatRoom != null && client != null)
                {

                    client.CurrentActiveChatRoom = Guid.Parse(chatroomId);
                    chatRoom?.ActiveClientsIds?.Add(client.Id);

                    lock (_lock) ActiveClients[client.Id].CurrentActiveChatRoom = Guid.Parse(chatroomId);

                    if (ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime.ContainsKey(Guid.Parse(chatroomId)))
                    {
                        lock (_lock) ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime[Guid.Parse(chatroomId)] = ActiveChatRooms[Guid.Parse(chatroomId)].CreationDate;
                    }
                    else
                    {
                        lock (_lock) ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime.TryAdd(Guid.Parse(chatroomId), ActiveChatRooms[Guid.Parse(chatroomId)].CreationDate);
                    }
                    if (!ActiveChatRooms[Guid.Parse(chatroomId)].ActiveClientsIds.Contains(client.Id))
                    {
                        lock (_lock) ActiveChatRooms[Guid.Parse(chatroomId)].ActiveClientsIds.Add(client.Id);
                    }

                    //TODO: Notify Room Memebers

                    //SendLastSyncMessages
                    SendMessageToClient(client, ConstructChatRoomSyncMessage(chatRoom, client));
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static void CreateNewChatRoom(string chatroomName, Message? m)
        {
            try
            {
                if (m is null || string.IsNullOrEmpty(chatroomName)) return;

                ActiveClients.TryGetValue(m?.ClientID ?? Guid.NewGuid(), out var client);
                if (client is null)
                {
                    LogError("Client Not Found!");
                }

                m.UserName = SystemMessage;
                m.ChatRoomID = GeneralChatRoomID;
                m.messageTypeEnum = MessageTypeEnum.MessageInformation;
                m.SentTime=DateTime.UtcNow;
                m.ReceivedTime = DateTime.UtcNow;
                //Check Existed Chat Room Name
                if (ChatRoomsNamesDic.ContainsKey(chatroomName.Trim().ToLower()))
                {
                    LogError($"Existed ChatRoom with same name {chatroomName}");
                    m.MessageText = $"Error: Cannot Create this chatroom with existed name: {chatroomName}";
                }
                else
                {
                    //Create Chat Room
                    Guid newChatRoomId = Guid.NewGuid();
                    m.ChatRoomID = newChatRoomId;
                    m.MessageId = Guid.NewGuid();
                    if (ActiveChatRooms.TryAdd(newChatRoomId, new ChatRoom() { id = newChatRoomId, CreationDate = DateTime.UtcNow, ActiveClientsIds = new List<Guid>(), MessagesList = new List<Message>(), RoomName = chatroomName }))
                    {

                        //Join Client to the Created ChatRoom
                        lock (_lock) ActiveClients[client.Id].CurrentActiveChatRoom = newChatRoomId;
                        lock (_lock) ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime.Add(newChatRoomId, DateTime.UtcNow);
                        lock (_lock) ChatRoomsNamesDic.TryAdd(chatroomName.Trim().ToLower(), newChatRoomId);

                        //Welcome Message
                        LogInfo(m.MessageText = $"Congratulations, New chatroom has been created with ID {newChatRoomId}");
                        lock (_lock) ActiveChatRooms[newChatRoomId]?.MessagesList?.Add(m);//Add message to the ChatRoom
                        lock (_lock) ActiveChatRooms[newChatRoomId]?.ActiveClientsIds?.Add(client.Id);//Add Client to the ChatRoom


                    }
                }
                SendMessageToClient(client, m);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static void LeaveAllChatRooms(Message? m)
        {
            try
            {
                if (m is null) return;

                ActiveClients.TryGetValue(m?.ClientID ?? Guid.NewGuid(), out var client);
                if (client is null)
                {
                    LogError("Client Not Found!");
                }

                var targetChatRooms = ActiveChatRooms.Where(x => x.Value.ActiveClientsIds.Contains(client.Id) && x.Key != GeneralChatRoomID);
                if (targetChatRooms != null)
                {
                    foreach (var cr in targetChatRooms)
                    {
                        lock (_lock) cr.Value.ActiveClientsIds?.Remove(client.Id);
                        client.CurrentActiveChatRoom = GeneralChatRoomID;
                        lock (_lock) ActiveClients[client.Id].CurrentActiveChatRoom = GeneralChatRoomID;
                        m.MessageText = $"Congratulations {client.Username},You've left all ChatRooms and switched to General ChatRoom {GeneralChatRoomID} Successfully";
                    }
                }

                m.UserName = SystemMessage;
                m.ChatRoomID = GeneralChatRoomID;
                m.messageTypeEnum = MessageTypeEnum.MessageInformation;
                SendMessageToClient(client, m);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private static void LeaveChatRoom(string chatroomId, Message? m)
        {
            try
            {
                if (m is null || string.IsNullOrEmpty(chatroomId)) return;

                ActiveClients.TryGetValue(m?.ClientID ?? Guid.NewGuid(), out var client);
                if (client is null)
                {
                    LogError("Client Not Found!");
                }

                if (!ActiveChatRooms.ContainsKey(Guid.Parse(chatroomId)) || !client.ActiveChatRoomWithLastSyncDateTime.ContainsKey(Guid.Parse(chatroomId)))
                {
                    m.MessageText = $"Your are not registered to the ChatRoom {chatroomId}";
                }
                else
                {
                    lock (_lock) ActiveChatRooms[Guid.Parse(chatroomId)]?.ActiveClientsIds?.Remove(client.Id);//Remove from recieving Room Messages
                    if (client?.CurrentActiveChatRoom == Guid.Parse(chatroomId)) //Remove chat room from client
                    {
                        client.CurrentActiveChatRoom = GeneralChatRoomID;
                        m.MessageText = $"You've switched to General ChatRoom {GeneralChatRoomID} Successfully";
                    }
                    else
                    {
                        m.MessageText = $"You've un subscribed from ChatRoom {chatroomId} Successfully";
                    }
                }

                m.UserName = SystemMessage;
                m.ChatRoomID = GeneralChatRoomID;
                m.messageTypeEnum = MessageTypeEnum.MessageInformation;
                SendMessageToClient(client, m);
            }
            catch (Exception)
            {

                throw;
            }
        }


        private static Message ConstructChatRoomSyncMessage(ChatRoom chatRoom, Client client)
        {
            try
            {
                DateTime LastSyncDate;
                if (ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime.ContainsKey(chatRoom.id))
                {
                    LastSyncDate = ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime[chatRoom.id];
                    ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime[chatRoom.id]=DateTime.UtcNow;
                }
                else
                {
                    ActiveClients[client.Id].ActiveChatRoomWithLastSyncDateTime.Add(chatRoom.id, DateTime.UtcNow);
                    LastSyncDate = ActiveChatRooms[chatRoom.id].CreationDate;
                }

                StringBuilder CRM = new StringBuilder("You Missed the next messages:\n");
                var messagesToSend = ActiveChatRooms[chatRoom.id].MessagesList.Where(x => x.SentTime >= LastSyncDate);
                foreach (var item in messagesToSend)
                {
                    CRM.AppendLine($"{item.ReceivedTime}/{item.UserName}:{item.MessageText}");
                }

                Message m = new Message();
                m.MessageText = CRM.ToString();
                m.messageTypeEnum = MessageTypeEnum.MessageInformation;
                m.UserName = SystemMessage;
                m.ChatRoomID = chatRoom.id;
                return m;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static void SendAvailableChatRooms(Message? m)
        {
            try
            {
                ActiveClients.TryGetValue(m?.ClientID ?? Guid.NewGuid(), out var client);
                if (client != null)
                {
                    StringBuilder sb = new StringBuilder("Available Chat Rooms:\n");
                    foreach (var item in ActiveChatRooms)
                    {
                        sb.AppendLine($"{item.Key}:{item.Value.RoomName}");
                    }

                    m.MessageText = sb.ToString();
                    m.UserName = SystemMessage;
                    m.ChatRoomID = GeneralChatRoomID;
                    SendMessageToClient(client, m);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }


        private static void SendMyAvailableChatRooms(Message? m)
        {
            try
            {
                ActiveClients.TryGetValue(m?.ClientID ?? Guid.NewGuid(), out var client);
                if (client != null)
                {
                    var clientActiveRooms = ActiveChatRooms.Where(x => x.Value.ActiveClientsIds.Contains(client.Id)).ToList();
                    StringBuilder sb = new StringBuilder("Your Active Chat Rooms:\n");
                    foreach (var item in clientActiveRooms)
                    {
                        sb.AppendLine($"{item.Key}:{item.Value.RoomName}");
                    }

                    m.MessageText = sb.ToString();
                    m.UserName = SystemMessage;
                    m.ChatRoomID = GeneralChatRoomID;
                    SendMessageToClient(client, m);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public static void Broadcast(string data)
        {
            try
            {
                Message? m = JsonSerializer.Deserialize<Message>(data);
                if (m.ClientID != null)
                {
                    var clientActiveChatRoom = ActiveClients[(Guid)m.ClientID].CurrentActiveChatRoom;
                    if (clientActiveChatRoom != null)
                    {
                        var targetChatRoom = ActiveChatRooms[(Guid)clientActiveChatRoom];
                        if (targetChatRoom != null)
                        {
                            var _Clients = targetChatRoom.ActiveClientsIds;
                            if (_Clients.Count > 0)
                            {
                                m.ChatRoomID = targetChatRoom.id;
                                m.ReceivedTime=DateTime.UtcNow;
                                targetChatRoom.MessagesList.Add(m);
                                lock (_lock) ActiveChatRooms[targetChatRoom.id]?.MessagesList?.Add(m);
                                foreach (var client in _Clients)
                                {
                                    Client c = ActiveClients[client];
                                    if (c != null && c.Id != m.ClientID && c.CurrentActiveChatRoom == targetChatRoom.id)
                                    {
                                        NetworkStream? stream = c.ClientConnection?.GetStream();
                                        string jsonString = JsonSerializer.Serialize(m);
                                        byte[] buffer = Encoding.ASCII.GetBytes(jsonString + Environment.NewLine);
                                        stream.Write(buffer, 0, buffer.Length);
                                        lock (_lock) c.ActiveChatRoomWithLastSyncDateTime[(Guid)clientActiveChatRoom] = DateTime.UtcNow;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void SendMessageToClient(Client? client, Message? message)
        {
            try
            {
                if (client is null || message is null) return;
                if (message.messageTypeEnum is null)
                    message.messageTypeEnum = MessageTypeEnum.MessageText;
                string jsonString = JsonSerializer.Serialize(message);
                byte[] buffer = Encoding.ASCII.GetBytes(jsonString);
                lock (_lock)
                {
                    NetworkStream? stream = client?.ClientConnection?.GetStream();
                    stream?.Write(buffer, 0, buffer.Length);
                    stream?.Flush();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static void SendHandShakeMessageToClient(Client client)
        {
            try
            {
                HandShakeMessage message = new HandShakeMessage();
                message.WelcomeMessage = $"Welcome to MMA Chat App your Client ID is {client.Id} and you Joined the General Chat Room!";
                message.ClientId = client.Id;
                message.MenuCommands = MenuCommands;
                string jsonString = JsonSerializer.Serialize(message);
                byte[] buffer = Encoding.ASCII.GetBytes(jsonString);
                lock (_lock)
                {
                    NetworkStream? stream = client?.ClientConnection?.GetStream();
                    stream?.Write(buffer, 0, buffer.Length);
                    stream?.Flush();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool Start(string ServerName)
        {
            return true;
        }

        public void Stop(int ServerId)
        {
            System.Environment.Exit(0);
        }

        public static void LogMessage(string message)
        {
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine(message);
            Console.WriteLine("--------------------------------------------------------------------");
        }

        public static void LogError(string message)
        {
            LogMessage(_log.Error(message));
        }

        public static void LogInfo(string message)
        {
            LogMessage(_log.Info(message));
        }

        public static void LogSystemMessage(string message)
        {
            LogMessage(_log.SystemMsg(message));
        }
        public static void DisplayCommandMenu()
        {
            LogMessage(MenuCommands);
        }

    }
}

