using Microsoft.AspNetCore.SignalR.Client;
using mma.types;

namespace mma.rest.client
{
    public class mmaClient
    {
        public string? ClientName { get; set; }
        public static Guid ClientId { get; set; }
        public static string? AcvtiveChatRoomId { get; set; }
        public static string? UserName { get; set; }
        private static Logger _log = new Logger();
        private static DateTime? lastSyncDateTime = null;
        public const string CommandSplittor = "!";
        private readonly int ServerPort = int.Parse(Environment.GetEnvironmentVariable("SERVERPORT", EnvironmentVariableTarget.Process) ?? "6869");
        private readonly string ServerIpAddress = Environment.GetEnvironmentVariable("SERVERIP", EnvironmentVariableTarget.Process) ?? "localhost";
        private readonly string ChatHub = "MMAChatHub";
        private string MenuCommands = string.Empty;

        private HubConnection _connection;
        private HubConnectionBuilder _connectionBuilder;
        private bool isCompleted = false;

        public mmaClient()
        {

            try
            {
                string? UserMessage;
                ClientId = Guid.NewGuid();
                Console.Write("Provide UserName: ");
                ClientName = UserName = Console.ReadLine();

                Console.WriteLine($"Connecting to MMA server at {ServerIpAddress}:{ServerPort} at {DateTime.Now}");

                _connectionBuilder = new HubConnectionBuilder();
                _connection = _connectionBuilder.WithUrl($"https://{ServerIpAddress}:{ServerPort}/{ChatHub}".ToLower(), (opts) =>
                {
                    opts.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                            // always verify the SSL certificate
                            clientHandler.ServerCertificateCustomValidationCallback +=
                                (sender, certificate, chain, sslPolicyErrors) => { return true; };
                        return message;
                    };
                }).WithAutomaticReconnect().Build();
                _connection.StartAsync().Wait();
                
                //Listen To Server Messages
                ReceiveServerMessages(_connection);

                //Welcome Handshake
                if (GetWelcomeMessage(_connection).Result)
                {                    
                    //Thread ct = new Thread(() => ReceiveServerMessages(chatClient));
                    //ct.Start();

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
                                    SendServerCommand(UserMessage, _connection, mmaCommands.SHOW_ACTIVE_CHATROOMS);
                                    break;
                                case "MCD": //Show My Active ChatRooms
                                    SendServerCommand(UserMessage, _connection, mmaCommands.SHOW_MY_CHATROOMS);
                                    break;
                                case "CLA": //Leave All ChatRooms
                                    SendServerCommand(UserMessage, _connection, mmaCommands.LEAVE_ALL_CHATROOMS);
                                    break;
                                case "CL": //Leave ChatRoom
                                    SendServerCommand(UserMessage, _connection, mmaCommands.LEAVE_CHATROOM);
                                    break;
                                case "CC": //Create New ChatRoom
                                    SendServerCommand(UserMessage, _connection, mmaCommands.CREATE_NEW_CHATROOM);
                                    break;
                                case "CJ": //Join Active ChatRoom
                                    SendServerCommand(UserMessage, _connection, mmaCommands.JOIN_ACTIVE_CHATROOM);
                                    break;
                                case "SM":
                                case "?"://Show MenuItems
                                    SendServerCommand(UserMessage, _connection, mmaCommands.GET_MENU);
                                    break;
                                default://Not agreed command/ Send it as normal chat message
                                    SendChatMessage(BuildServerMessage(UserMessage), _connection);
                                    break;
                            }
                        }
                        else
                        {
                            //Send Message
                            SendChatMessage(BuildServerMessage(UserMessage), _connection);
                        }
                    }
                    //ct.Join();
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
        private async void SendServerCommand(string userMessage, HubConnection chatClient, mmaCommands commandType)
        {
            var messageSplitted = userMessage?.Split(CommandSplittor);
            var commandParameters = string.Empty;
            if (messageSplitted != null && !string.IsNullOrEmpty(messageSplitted[1].Trim()))
            {
                commandParameters = messageSplitted[1].Trim();
            }
            if (messageSplitted != null && messageSplitted.Length > 0)
            {
                await chatClient.SendAsync("SendCommand", new SendCommandRequest
                {
                    RequestHeader = buildRequestHeader(),
                    CommandMessage = new mmaCommandMessage
                    {
                        CommandType = commandType,
                        CommandParameters = commandParameters,
                        SentTime = DateTime.UtcNow
                    }
                });
            }
        }

        private async void SendChatMessage(mmaChatMessage userMessage, HubConnection chatClient)
        {
            await chatClient.SendAsync("SendMessage", new SendMessageRequest { RequestHeader = buildRequestHeader(), ChatMessage = userMessage });
            //Console.WriteLine($"{UserName}> ");
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
                SentTime = DateTime.UtcNow
            };
            return servermessage;
        }


        private mmaRequestHeader buildRequestHeader()
        {
            return new mmaRequestHeader
            {
                ClientId = ClientId.ToString(),
                ClientName = this.ClientName,
                SentTime = DateTime.UtcNow
            };
        }

        private async Task<bool> GetWelcomeMessage(HubConnection client)

        {
            // SendHandshakeMessage
            await client.InvokeCoreAsync("SendHSMessage", args: new[]{new SendHSMessageRequest
            {
                HSMessage = new mmaHandShakeMessage
                {
                    ClientId = ClientId.ToString(),
                    ClientName = this.ClientName,
                    MessageId = Guid.NewGuid().ToString(),
                    SentTime = DateTime.UtcNow,
                    MessageText = "This is Client First Message"
                },
                RequestHeader = buildRequestHeader()
            }});

            return true;
        }

        private void ReceiveServerMessages(HubConnection client)
        {
            try
            {
                _connection.On<string>("Connected", (string message) =>
                {
                    DisplayMessage(message);
                });

                ////Listen To Server Messages                
                _connection.On<mmaChatMessage>("RecieveMessage", (mmaChatMessage messageContent) =>
                {
                    var m = messageContent;
                    DisplayMessage($"{m.ChatRoomId}/{m.UserName}/{m.SentTime.ToShortTimeString()}> {m.MessageText}");
                    lastSyncDateTime = DateTime.UtcNow;
                    Console.WriteLine($"{UserName}> ");
                });

                _connection.On<SendHSMessageReply>("ReceiveHandShakeMessage", (SendHSMessageReply HSMessageReply) =>
                {
                    if (HSMessageReply?.ACKMessage?.Status == mmaRequestStatus.SUCCESS)
                    {
                        AcvtiveChatRoomId = HSMessageReply?.ReplyHeader?.GeneralChatRoomId;
                        this.MenuCommands = HSMessageReply?.ACKMessage?.AckString ?? MenuCommands;
                        DisplayMessage(HSMessageReply?.HSReplyMessage!);
                        DisplayMessage(HSMessageReply?.ACKMessage.AckString!);
                        Console.Write($"{UserName}> ");
                    }
                    else
                    {
                        DisplayMessage("HandShake Failed!!!!!");
                    }
                });

                _connection.On<SendCommandReply>("ReceiveCommandResult", (SendCommandReply commandResult) =>
                {
                    if (commandResult?.ACKMessage?.Status == mmaRequestStatus.SUCCESS)
                    {
                        DisplayMessage($"{commandResult?.ReplyHeader?.ServerName} Success Message:> {commandResult?.ACKMessage.AckString}");
                    }
                    else
                    {
                        DisplayMessage($"{commandResult?.ReplyHeader?.ServerName} Error Message:> {commandResult?.ACKMessage?.AckString}");
                    }
                    Console.WriteLine($"{UserName}> ");
                });


                _connection.On<RecieveMessageStreamReply>("SendMessageStream", (RecieveMessageStreamReply Reply) =>
                {
                    var m = Reply.Message;
                    DisplayMessage($"{m.ChatRoomId}/{m.UserName}/{m.SentTime.ToShortTimeString()}> {m.MessageText}");
                    lastSyncDateTime = DateTime.UtcNow;
                    Console.WriteLine($"{UserName}> ");
                });


                //_connection.StartAsync().GetAwaiter().GetResult();
                //while (!isCompleted)
                //{
                //    Task.Delay(10).GetAwaiter().GetResult();
                //}

                //while (true)
                //{
                //    // StreamRecieveMessages
                //    await client.SendAsync("RecieveMessageStream", new RecieveMessageStreamRequest
                //    {
                //        LastSyncDateTime = lastSyncDateTime == null ? DateTime.Now.AddMonths(-5) : (DateTime)lastSyncDateTime,
                //        RequestHeader = buildRequestHeader()
                //    });
                //}
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
