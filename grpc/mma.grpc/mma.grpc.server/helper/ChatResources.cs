using Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;
using System.Text;

namespace mma.grpc.server.helper
{
    public class ChatResources : IChatResources
    {
        public ConcurrentDictionary<Guid, mmaClient?>? ActiveClients;

        public ConcurrentDictionary<Guid, mmaChatRoom?>? ActiveChatRooms;

        public ConcurrentDictionary<Guid, mmaNotifcationMessage?>? SystemMessages;

        static readonly object _lock = new object();
        private readonly Guid GeneralChatRoomID = Guid.NewGuid();
        private readonly Guid ServerId = Guid.NewGuid();
        private const string GeneralChatRoomName = "MMA General ChatRoom";
        private const string ServerName = "MMA Server";

        string IChatResources.GeneralChatRoomName => GeneralChatRoomName;
        string IChatResources.GeneralChatRoomID => GeneralChatRoomID.ToString();
        string IChatResources.ServerId => ServerId.ToString();
        string IChatResources.ServerName => ServerName;

        public ChatResources()
        {
            ActiveClients = new ConcurrentDictionary<Guid, mmaClient?>();
            ActiveChatRooms = new ConcurrentDictionary<Guid, mmaChatRoom?>();
            SystemMessages = new ConcurrentDictionary<Guid, mmaNotifcationMessage?>();
            mmaChatRoom generalChatRoom = new mmaChatRoom() { ChatRoomName = GeneralChatRoomName, CreationTime = Timestamp.FromDateTime(DateTime.UtcNow), Id = GeneralChatRoomID.ToString(), IsActive = true };
            ActiveChatRooms.TryAdd(Guid.Parse(generalChatRoom.Id), generalChatRoom);
        }

        public void addActiveChatRooms(Guid chatRoomId, mmaChatRoom chatRoom)
        {
            lock (_lock) ActiveChatRooms?.TryAdd(chatRoomId, chatRoom);
        }

        public void addActiveClient(Guid clientId, mmaClient? client)
        {
            lock (_lock) ActiveClients?.TryAdd(clientId, client);
            lock (_lock) ActiveChatRooms?[GeneralChatRoomID]?.ChatRoomClients?.Add(client);
        }

        public mmaChatRoom? getActiveChatRoom(Guid chatRoomId)
        {
            return ActiveChatRooms?[chatRoomId];
        }

        public mmaClient? getActiveClient(Guid clientId)
        {
            return ActiveClients?[clientId];
        }

        public void removeActiveChatRooms(Guid chatRoomId)
        {
            throw new NotImplementedException();
        }

        public void removeActiveClient(Guid clientId)
        {
            throw new NotImplementedException();
        }

        public void updateActiveChatRooms(Guid chatRoomId, mmaChatRoom chatRoom)
        {
            throw new NotImplementedException();
        }

        public void updateActiveClient(Guid clientId, mmaClient? client)
        {
            throw new NotImplementedException();
        }

        public void addMessageToChatRoom(Guid chatRoomId, mmaChatMessage? message)
        {
            var targetChatRoom = getActiveChatRoom(chatRoomId);
            if (targetChatRoom != null)
            {
                targetChatRoom.ChatRoomMessages.Add(message);
            }
        }

        public void UpdateClientSyncDateTimeForChatRoom(string? clientActiveChatRoom, string? clientRequesterId)
        {
            if (string.IsNullOrEmpty(clientRequesterId) || string.IsNullOrEmpty(clientActiveChatRoom) || ActiveChatRooms is null || ActiveClients is null)
            {
                return;
            }


            var c = ActiveChatRooms?[Guid.Parse(clientActiveChatRoom)]?.ChatRoomClients?.FirstOrDefault(c => c.ClientId.Equals(clientRequesterId));
            if (c != null)
            {
                lock (_lock)
                {
                    c.LastSyncDateTime = Timestamp.FromDateTime(DateTime.UtcNow);
                }
            }
        }

        public bool addClientToChatRoom(string chatRoomId, string rClientId)
        {

            if (string.IsNullOrEmpty(chatRoomId) || string.IsNullOrEmpty(rClientId) || ActiveChatRooms is null || ActiveClients is null)
            {
                return false;
            }

            var client = ActiveClients[Guid.Parse(rClientId)];
            if (client is null) return false;
            var chatRoom = ActiveChatRooms[Guid.Parse(chatRoomId)];
            if (chatRoom is null) return false;
            client.ActiveChatRoom = chatRoom?.Id;
            client.LastSyncDateTime = chatRoom?.CreationTime;
            ActiveChatRooms?[Guid.Parse(chatRoomId)]?.ChatRoomClients.Add(client);

            ActiveChatRooms[Guid.Parse(chatRoomId)]?.ChatRoomMessages.Add(new mmaChatMessage
            {
                ChatRoomId = chatRoomId,
                ClientId = ServerId.ToString(),
                MessageId = Guid.NewGuid().ToString(),
                MessageText = $"<<<< {client?.Name} joined the room >>>>",
                SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                ReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow),
                UserName = ServerName
            });

            return true;
        }

        public string getChatRoomName(string chatRoomId)
        {
            if (string.IsNullOrEmpty(chatRoomId)) return string.Empty;

            return ActiveChatRooms?[Guid.Parse(chatRoomId)]?.ChatRoomName ?? string.Empty;
        }

        public bool createNewChatRoom(string cParameters, string rClientId)
        {
            try
            {
                if (cParameters is null || string.IsNullOrEmpty(rClientId) || ActiveClients == null || ActiveChatRooms == null) return false;
                var checkChatRoomWithSameNameExist = ActiveChatRooms?.Where(r => r.Value != null && r.Value.ChatRoomName.ToLower().Equals(cParameters.ToLower())).ToList();

                if (checkChatRoomWithSameNameExist?.Count() > 0)
                {
                    return false;
                }
                else
                {
                    var c = ActiveClients[Guid.Parse(rClientId)];
                    if (c is null) return false;
                    var newChatRoomID = Guid.NewGuid();

                    mmaChatRoom mmaChatRoom = new mmaChatRoom
                    {
                        ChatRoomName = cParameters,
                        CreationTime = Timestamp.FromDateTime(DateTime.UtcNow),
                        Id = newChatRoomID.ToString(),
                        IsActive = true,
                    };
                    c.LastSyncDateTime = mmaChatRoom.CreationTime;
                    c.ActiveChatRoom = newChatRoomID.ToString();
                    mmaChatRoom.ChatRoomClients.Add(c);


                    //Create Chat Room
                    if (ActiveChatRooms != null)
                    {
                        lock (_lock) ActiveChatRooms.TryAdd(newChatRoomID, mmaChatRoom);
                        if (ActiveClients != null && ActiveClients[Guid.Parse(rClientId)] != null)
                            lock (_lock) ActiveClients[Guid.Parse(rClientId)].ActiveChatRoom = newChatRoomID.ToString();

                        ActiveChatRooms[newChatRoomID]?.ChatRoomMessages.Add(new mmaChatMessage
                        {
                            ChatRoomId = cParameters,
                            ClientId = ServerId.ToString(),
                            MessageId = Guid.NewGuid().ToString(),
                            MessageText = $"<<<< {c?.Name} Created this chat Room >>>>",
                            SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                            ReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow),
                            UserName = ServerName
                        });

                        ActiveChatRooms[GeneralChatRoomID]?.ChatRoomMessages.Add(new mmaChatMessage
                        {
                            ChatRoomId = cParameters,
                            ClientId = ServerId.ToString(),
                            MessageId = Guid.NewGuid().ToString(),
                            MessageText = $"<<<< new ChatRoom {cParameters} created by {c?.Name} with id {newChatRoomID.ToString()}>>>>",
                            SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                            ReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow),
                            UserName = ServerName
                        });

                    }
                    return true;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool removeClientFromChatRoom(string cParameters, string rClientId)
        {
            try
            {
                if (string.IsNullOrEmpty(cParameters) || string.IsNullOrEmpty(rClientId) || ActiveChatRooms == null || ActiveClients == null) return false;

                if (!ActiveChatRooms.ContainsKey(Guid.Parse(cParameters)) || ActiveChatRooms.Where(x => x.Value != null && x.Key == Guid.Parse(cParameters) && x.Value.ChatRoomClients.Where(c => c.ClientId.Equals(rClientId)).Count() > 0).Count() == 0)
                {
                    return false;
                }
                else
                {
                    var c = ActiveChatRooms[Guid.Parse(cParameters)]?.ChatRoomClients.Where(c => c.ClientId.Equals(rClientId)).FirstOrDefault();
                    lock (_lock) ActiveChatRooms[Guid.Parse(cParameters)]?.ChatRoomClients?.Remove(c);//Remove from recieving Room Messages

                    if (ActiveClients != null && ActiveClients[Guid.Parse(rClientId)].ActiveChatRoom.Equals(cParameters)) //Remove chat room from client
                    {
                        ActiveClients[Guid.Parse(rClientId)].ActiveChatRoom = GeneralChatRoomID.ToString();
                    }
                    ActiveChatRooms[Guid.Parse(cParameters)]?.ChatRoomMessages.Add(new mmaChatMessage
                    {
                        ChatRoomId = cParameters,
                        ClientId = ServerId.ToString(),
                        MessageId = Guid.NewGuid().ToString(),
                        MessageText = $"<<<<{c?.Name} Left the chat room>>>>",
                        SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                        ReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow),
                        UserName = ServerName
                    });
                    return true;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool removeClientFromAllChatRooms(string rClientId)
        {
            try
            {
                if (rClientId is null) return false;

                var targetChatRooms = ActiveChatRooms?.Where(x => x.Value != null && x.Value.ChatRoomClients.Where(c => c.ClientId == rClientId).Count() > 0 && x.Key != GeneralChatRoomID);
                if (targetChatRooms != null)
                {
                    foreach (var cr in targetChatRooms)
                    {
                        if (cr.Value != null)
                            foreach (var c in cr.Value.ChatRoomClients.Where(c => c.ClientId.Equals(rClientId)))
                            {
                                lock (_lock) cr.Value.ChatRoomClients.Remove(c);
                                cr.Value.ChatRoomMessages.Add(new mmaChatMessage
                                {
                                    ChatRoomId = cr.Value.Id,
                                    ClientId = ServerId.ToString(),
                                    MessageId = Guid.NewGuid().ToString(),
                                    MessageText = $"<<<<{c?.Name} Left the chat room>>>>",
                                    SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                                    ReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow),
                                    UserName = ServerName
                                });
                            }
                    }

                    if (ActiveClients != null && rClientId != null && ActiveClients[Guid.Parse(rClientId)] != null)
                        lock (_lock) ActiveClients[Guid.Parse(rClientId)].ActiveChatRoom = GeneralChatRoomID.ToString();
                    return true;
                }

                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string getClientChatRooms(string rClientId)
        {
            try
            {
                if (rClientId != null)
                {
                    var clientActiveRooms = ActiveChatRooms?.Where(c => c.Value != null && c.Value.IsActive == true && c.Value.ChatRoomClients.Where(x => x.ClientId == rClientId).Count() > 0).ToList();
                    StringBuilder sb = new StringBuilder("Your Active Chat Rooms:\n");
                    if (clientActiveRooms != null)
                        foreach (var item in clientActiveRooms)
                        {
                            if (item.Value != null)
                                sb.AppendLine($"{item.Key}:{item.Value.ChatRoomName}");
                        }

                    return sb.ToString();
                }

                return string.Empty;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string getAllActiveChatRooms(string rClientId)
        {
            try
            {
                if (rClientId != null)
                {
                    StringBuilder sb = new StringBuilder("Available Chat Rooms:\n");

                    var activeRooms = ActiveChatRooms?.Where(c => c.Value != null && c.Value.IsActive == true).ToList();
                    if (activeRooms != null)
                        foreach (var item in activeRooms)
                        {
                            if (item.Value != null)
                                if (item.Value.ChatRoomClients.Where(x => x.ClientId == rClientId).Count() > 0)
                                    sb.AppendLine($"{item.Key}:{item.Value?.ChatRoomName} --Registerd");
                                else
                                    sb.AppendLine($"{item.Key}:{item.Value?.ChatRoomName}");
                        }
                    return sb.ToString();
                }
                return string.Empty;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }


    public interface IChatResources
    {
        string GeneralChatRoomName { get; }
        string GeneralChatRoomID { get; }
        string ServerId { get; }
        string ServerName { get; }

        void addActiveChatRooms(Guid chatRoomId, mmaChatRoom chatRoom);
        void updateActiveChatRooms(Guid chatRoomId, mmaChatRoom chatRoom);
        void removeActiveChatRooms(Guid chatRoomId);
        mmaChatRoom? getActiveChatRoom(Guid chatRoomId);
        void addMessageToChatRoom(Guid chatRoomId, mmaChatMessage? message);



        void addActiveClient(Guid clientId, mmaClient? client);
        void updateActiveClient(Guid clientId, mmaClient? client);
        void removeActiveClient(Guid clientId);
        mmaClient? getActiveClient(Guid clientId);
        void UpdateClientSyncDateTimeForChatRoom(string? clientActiveChatRoom, string? clientRequesterId);
        bool addClientToChatRoom(string chatRoomId, string rClientId);
        string getChatRoomName(string chatRoomId);
        bool createNewChatRoom(string cParameters, string rClientId);
        bool removeClientFromChatRoom(string cParameters, string rClientId);
        bool removeClientFromAllChatRooms(string rClientId);
        string getClientChatRooms(string rClientId);
        string getAllActiveChatRooms(string rClientId);
    }
}
