using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using mma.grpc.server;

namespace mma.grpc.client
{
    public class mmaGrpcClient
    {
        public string? ClientName { get; set; }
        public static Guid ClientId { get; set; }
        public static string? AcvtiveChatRoomId { get; set; }
        public static string? UserName { get; set; }
        private static Logger _log = new Logger();
        private static DateTime? lastSyncDateTime = null;
        public const string CommandSplittor = "!";
        private readonly int ServerPort = int.Parse(Environment.GetEnvironmentVariable("SERVERPORT", EnvironmentVariableTarget.Process) ?? "49163");
        private readonly string ServerIpAddress = Environment.GetEnvironmentVariable("SERVERIP", EnvironmentVariableTarget.Process) ?? "localhost";
        private string MenuCommands = string.Empty;

        public mmaGrpcClient()
        {
            try
            {
                string? UserMessage;
                ClientId = Guid.NewGuid();
                Console.Write("Provide UserName: ");
                ClientName = UserName = Console.ReadLine()??"Ahmed";

                Console.WriteLine($"Connecting to MMA server at {ServerIpAddress}:{ServerPort} at {DateTime.Now}");
                using var channel = GrpcChannel.ForAddress($"https://{ServerIpAddress}:{ServerPort}/");
                var chatClient = new mmaChatService.mmaChatServiceClient(channel);

                //Welcome Handshake
                if (GetWelcomeMessage(chatClient))
                {
                    //Listen To Server Messages                
                    Thread ct = new Thread(() => ReceiveServerMessages(chatClient));
                    ct.Start();

                    while (!string.IsNullOrEmpty(UserMessage = Console.ReadLine()))
                    {

                        // Send JOIN Reuqest
                        Console.WriteLine($"{UserName}> ");
                        var messageSplitted = UserMessage.Split(CommandSplittor);
                        if (messageSplitted != null && messageSplitted.Length > 0)
                        {
                            var command = messageSplitted[0].ToUpper();
                            switch (command)
                            {
                                case "CD": //Show Active ChatRooms
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.ShowActiveChatrooms);
                                    break;
                                case "MCD": //Show My Active ChatRooms
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.ShowMyChatrooms);
                                    break;
                                case "CLA": //Leave All ChatRooms
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.LeaveAllChatrooms);
                                    break;
                                case "CL": //Leave ChatRoom
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.LeaveChatroom);
                                    break;
                                case "CC": //Create New ChatRoom
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.CreateNewChatroom);
                                    break;
                                case "CJ": //Join Active ChatRoom
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.JoinActiveChatroom);
                                    break;
                                case "SM":
                                case "?"://Show MenuItems
                                    SendServerCommand(UserMessage, chatClient, mmaCommands.GetMenu);
                                    break;
                                default://Not agreed command/ Send it as normal chat message
                                    SendChatMessage(BuildServerMessage(UserMessage), chatClient);
                                    break;
                            }
                        }
                        else
                        {
                            //Send Message
                            SendChatMessage(BuildServerMessage(UserMessage), chatClient);
                        }
                    }
                    ct.Join();
                    // Send Logout Request
                    Console.WriteLine("disconnect from server!!");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Send Logout Request
                throw;
            }

        }

        private void SendServerCommand(string userMessage, mmaChatService.mmaChatServiceClient chatClient, mmaCommands commandType)
        {
            var messageSplitted = userMessage?.Split(CommandSplittor);
            var commandParameters = string.Empty;
            if (messageSplitted != null && !string.IsNullOrEmpty(messageSplitted[1].Trim()))
            {
                commandParameters = messageSplitted[1].Trim();
            }
            if (messageSplitted != null && messageSplitted.Length > 0)
            {
                var commandResult = chatClient.SendCommandAsync(new SendCommandRequest
                {
                    RequestHeader = buildRequestHeader(),
                    CommandMessage = new mmaCommandMessage
                    {
                        CommandType = commandType,
                        CommandParameters = commandParameters,
                        SentTime = Timestamp.FromDateTime(DateTime.UtcNow)
                    }
                });

                if (commandResult != null)
                {
                    if (commandResult.ResponseAsync.Result.ACKMessage.Status == mmaRequestStatus.Success)
                    {
                        DisplayMessage($"{commandResult.ResponseAsync.Result.ReplyHeader.ServerName} Success Message:> {commandResult.ResponseAsync.Result.ACKMessage.AckString}");
                    }
                    else
                    {
                        DisplayMessage($"{commandResult.ResponseAsync.Result.ReplyHeader.ServerName} Error Message:> {commandResult.ResponseAsync.Result.ACKMessage.AckString}");
                    }
                    Console.WriteLine($"{UserName}> ");
                }
            }
        }

        private void SendChatMessage(mmaChatMessage userMessage, mmaChatService.mmaChatServiceClient chatClient)
        {
            var sendMsgReplhy = chatClient.SendMessageAsync(new SendMessageRequest { RequestHeader = buildRequestHeader(), ChatMessage = userMessage });
            if (sendMsgReplhy.ResponseAsync.Result.ACKMessage.Status == mmaRequestStatus.Failed)
            {
                DisplayMessage("Failed to send the message");
            }
        }

        private mmaChatMessage BuildServerMessage(string message)
        {
            var servermessage = new mmaChatMessage()
            {
                MessageText = message,
                ChatRoomId = AcvtiveChatRoomId ?? null,
                ClientId = ClientId.ToString(),
                UserName = UserName ?? string.Empty,
                MessageId = Guid.NewGuid().ToString(),
                SentTime = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            return servermessage;
        }


        private mmaRequestHeader buildRequestHeader()
        {
            return new mmaRequestHeader
            {
                ClientId = ClientId.ToString(),
                ClientName = this.ClientName,
                SentTime = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }

        private bool GetWelcomeMessage(mmaChatService.mmaChatServiceClient client)
        {
            // SendHandshakeMessage
            var HSMessageReply = client.SendHSMessage(new SendHSMessageRequest
            {
                HSMessage = new mmaHandShakeMessage
                {
                    ClientId = ClientId.ToString(),
                    ClientName = this.ClientName,
                    MessageId = Guid.NewGuid().ToString(),
                    SentTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    MessageText = "This is Client First Message"
                },
                RequestHeader = buildRequestHeader()
            });

            if (HSMessageReply.ACKMessage.Status == mmaRequestStatus.Success)
            {
                AcvtiveChatRoomId = HSMessageReply.ReplyHeader.GeneralChatRoomId;
                this.MenuCommands = HSMessageReply.ACKMessage.AckString;
                DisplayMessage(HSMessageReply.HSReplyMessage);
                DisplayMessage(HSMessageReply.ACKMessage.AckString);
                Console.Write($"{UserName}> ");
                return true;
            }
            else
            {
                DisplayMessage("HandShake Failed!!!!!");
            }
            return false;
        }

        private async void ReceiveServerMessages(mmaChatService.mmaChatServiceClient client)
        {
            try
            {
                while (true)
                {
                    // StreamRecieveMessages
                    var messagesStream = client.RecieveMessageStream(new RecieveMessageStreamRequest
                    {
                        LastSyncDateTime = Timestamp.FromDateTime(DateTime.SpecifyKind(lastSyncDateTime == null ? DateTime.Now.AddMonths(-5) : (DateTime)lastSyncDateTime, DateTimeKind.Utc)),
                        RequestHeader = buildRequestHeader()
                    });

                    int recievedMessageCount = 0;
                    while (await messagesStream.ResponseStream.MoveNext(new CancellationToken()))
                    {
                        var m = messagesStream.ResponseStream.Current.Message;
                        DisplayMessage($"{m.UserName}/{m.SentTime.ToDateTime().ToShortTimeString()}> {m.MessageText}");
                        recievedMessageCount++;
                    }
                    //Reset Last Sync Time
                    if (recievedMessageCount > 0)
                    {
                        lastSyncDateTime = DateTime.UtcNow;
                        Console.WriteLine($"{UserName}> ");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
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

        public static void DisplayMessage(string message)
        {
            LogMessage(message);
        }

        public static void LogSystemMessage(string message)
        {
            LogMessage(_log.SystemMsg(message));
        }
        public void DisplayCommandMenu()
        {
            Console.WriteLine(MenuCommands);
        }
    }
}
