using mma.common;
using mma.model;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace mma.client
{
    public class mmaClient
    {
        public string ClientName { get; set; }
        public Guid ClientId { get; set; }
        public static string UserName { get; set; }
        public ChatRoom ActiveChatRoom { get; set; }

        private static Logger _log = new Logger();
        private TcpClient client;
        private readonly int ServerPort = int.Parse(Environment.GetEnvironmentVariable("SERVERPORT", EnvironmentVariableTarget.Process)??"6565");
        private readonly string ServerIpAddress = Environment.GetEnvironmentVariable("SERVERIP", EnvironmentVariableTarget.Process)??"127.0.0.1";
        private string MenuCommands = $"Menu Commands:{Environment.NewLine}" +
            $"CD! ==>List Available ChatRooms {Environment.NewLine} " +
            $"CJ!RoomID ==> Join Chat Room {Environment.NewLine} " +
            $"CL!RoomID ==> Leave Chat Room {Environment.NewLine} " +
            $"CC!RoomName ==> Create New ChatRoom {Environment.NewLine}";

        public mmaClient()
        {
            try
            {
                Message ServerMessage;
                string UserMessage;
                
                Console.WriteLine($"Connecting to MMA server at {ServerIpAddress} : {ServerPort}");
              
                IPAddress ip = IPAddress.Parse(ServerIpAddress);
                client = new TcpClient();
                client.Connect(ServerIpAddress, ServerPort);
                Console.WriteLine($"Client connected to {ServerIpAddress} at {DateTime.Now}");
                //Welcome Handshake
                getWelcomeMessage(client);

                Console.Write("Provide UserName: ");
                this.ClientName = UserName = Console.ReadLine();
                //Listen To Server Messages                
                Thread ct = new Thread(() => ReceiveData(client));
                ct.Start();
                NetworkStream ns = client.GetStream();

                while (!string.IsNullOrEmpty(UserMessage = Console.ReadLine()))
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(BuildServerMessage(UserMessage));
                    ns.Write(buffer, 0, buffer.Length);
                    Console.Write($"{UserName}> ");
                }

                client.Client.Shutdown(SocketShutdown.Send);
                ct.Join();
                ns.Close();
                client.Close();
                Console.WriteLine("disconnect from server!!");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (client != null && client.Connected)
                {
                    client.Client.Shutdown(SocketShutdown.Send);
                    client.Close();
                }
                throw;
            }

        }

        private string BuildServerMessage(string message)
        {
            var servermessage = new Message()
            {
                MessageText = message,
                ChatRoomID = ActiveChatRoom?.id ?? null,
                ClientID = this.ClientId,
                UserName = UserName ?? string.Empty,
                MessageId = Guid.NewGuid(),
                SentTime = DateTime.UtcNow
            };
            string jsonString = JsonSerializer.Serialize(servermessage);
            return jsonString;
        }

        private string getWelcomeMessage(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;
            byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length);
            if (byte_count > 0)
            {
                var message = Encoding.ASCII.GetString(receivedBytes, 0, byte_count);
                HandShakeMessage m = JsonSerializer.Deserialize<HandShakeMessage>(message);
                if (m != null)
                {
                    this.ClientId = m.ClientId;
                    this.MenuCommands = m.MenuCommands;
                    LogInfo($"Client ID has been chnaged to:{m.ClientId}");
                    DisplayMessage($"{m.WelcomeMessage}");
                    DisplayMessage(m.MenuCommands);
                    Console.Write($"{UserName}> ");
                    return m.WelcomeMessage.ToString();
                }
                else
                {
                    throw new Exception("Handshake Failed");
                }
            }
            return string.Empty;
        }

        static void ReceiveData(TcpClient client)
        {
            try
            {
                NetworkStream ns = client.GetStream();
                byte[] receivedBytes = new byte[1024];
                int byte_count;

                while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
                {
                    var data = Encoding.ASCII.GetString(receivedBytes, 0, byte_count);

                    if (!string.IsNullOrEmpty(data))
                    {
                        Message m = JsonSerializer.Deserialize<Message>(data);
                        DisplayMessage($"{m.SentTime}/{m.UserName}: {m.MessageText}");
                        Console.Write($"{UserName}> ");
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

