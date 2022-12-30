using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace mma.orleans.client
{
    public static class ChatClient
    {
        public static readonly string generalChatRoom = "GeneralRoom";
        public static readonly string CommandSplittor = "!";
        private static readonly string MenuCommands = $"Menu Commands:{Environment.NewLine}" +
                $"CM!           ==> List ChatRoom Members {Environment.NewLine}" +
                $"CH!           ==> List Messages History {Environment.NewLine}" +
                $"SM!           ==> Show Menu Commands {Environment.NewLine}" +
                $"CL!           ==> Leave ChatRoom {Environment.NewLine}" +
                $"CJ![RoomName] ==> Join ChatRoom {Environment.NewLine}";
        public static async Task ProcessLoopAsync(ClientContext context)
        {
            Console.WriteLine(MenuCommands);
            context = await JoinChatRoom(context, generalChatRoom);
            string? input = null;
            do
            {
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input.StartsWith("EX!"))
                {
                    break;
                }

                var messageSplitted = input.Split(CommandSplittor);
                if (messageSplitted != null && messageSplitted.Length > 0)
                {
                    var command = messageSplitted[0].ToUpper();
                    if (command is "UN")
                    {
                        context = context with { UserName = input.Replace("UN!", "").Trim() };
                        Console.WriteLine("Set username to {0}", context.UserName);
                        continue;
                    }

                    if (command is "SM")
                    {
                        Console.WriteLine(MenuCommands);
                        continue;
                    }

                    if (command switch
                    {
                        "CJ" => JoinChatRoom(context, input.Replace("CJ!", "").Trim()),
                        "CL" => LeaveChatRoom(context),
                        _ => null
                    } is Task<ClientContext> cxtTask)
                    {
                        context = await cxtTask;
                        continue;
                    }

                    if (command switch
                    {
                        "CH" => ShowCurrentChatRoomMessagesHistory(context),
                        "CM" => ShowChatRoomMembers(context),
                        _ => null
                    } is Task task)
                    {
                        await task;
                        continue;
                    }
                }

                await SendMessage(context, input);
            } while (input is not "EX!");
        }

        static async Task ShowChatRoomMembers(ClientContext context)
        {
            var room = context.Client.GetGrain<IChatRoomGrain>(context.CurrentChatRoom);
            var members = await room.GetMembers();
            if (members != null && members.Length > 0)
            {
                foreach (var member in members)
                {
                    Console.WriteLine($"{context.CurrentChatRoom} member: {member}");
                }
            }
            else
            {
                Console.WriteLine("No members in this chatroom");
            }
        }

        static async Task ShowCurrentChatRoomMessagesHistory(ClientContext context)
        {
            var room = context.Client.GetGrain<IChatRoomGrain>(context.CurrentChatRoom);
            var history = await room.ReadHistory(1_000);
            foreach (var chatMsg in history)
            {
                Console.WriteLine("[{0}][{1}]: {2}", chatMsg.Created.LocalDateTime.ToShortTimeString(), chatMsg.Author, chatMsg.Text); ;
            }
        }

        static async Task SendMessage(ClientContext context, string messageText)
        {
            var room = context.Client.GetGrain<IChatRoomGrain>(context.CurrentChatRoom);
            await room.SendMessage(new ChatMsg(context.UserName, messageText));
        }

        static async Task<ClientContext> JoinChatRoom(ClientContext context, string chatRoomName)
        {
            if (context.CurrentChatRoom is not null
                && !string.Equals(context.CurrentChatRoom, generalChatRoom, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(context.CurrentChatRoom, chatRoomName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Leaving chatroom {0} before joining {1}", context.CurrentChatRoom, chatRoomName);
                await LeaveChatRoom(context);
            }

            Console.WriteLine("Joining ChatRoom {0}", chatRoomName);
            context = context with { CurrentChatRoom = chatRoomName };

            var room = context.Client.GetGrain<IChatRoomGrain>(context.CurrentChatRoom);
            await room.Join(context.UserName!);
            var streamId = StreamId.Create("ChatRoom", context.CurrentChatRoom!);
            var stream = context.Client.GetStreamProvider("MMAChat").GetStream<ChatMsg>(streamId);

            // Subscribe to the stream to receive furthur messages sent to the chatroom
            await stream.SubscribeAsync(new ChatMessageObserver(chatRoomName));

            Console.WriteLine("Joined ChatRoom {0}", context.CurrentChatRoom!);
            return context;
        }

        static async Task<ClientContext> LeaveChatRoom(ClientContext context)
        {
            if (context.CurrentChatRoom is null || string.Equals(context.CurrentChatRoom, generalChatRoom, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("You cannot leave ChatRoom {0}", context.CurrentChatRoom!);
                return context;
            }

            Console.WriteLine("Leaving ChatRoom {0}", context.CurrentChatRoom!);
            var room = context.Client.GetGrain<IChatRoomGrain>(context.CurrentChatRoom);
            await room.Leave(context.UserName!);

            var streamId = StreamId.Create("ChatRoom", context.CurrentChatRoom!);
            var stream = context.Client.GetStreamProvider("MMAChat").GetStream<ChatMsg>(streamId);

            // Unsubscribe from the ChatRoom/stream since client left, so that client won't
            // receive future messages from this ChatRoom/stream.
            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            foreach (var handle in subscriptionHandles)
            {
                await handle.UnsubscribeAsync();
            }

            Console.WriteLine("Left ChatRoom {0}", context.CurrentChatRoom!);
            context = context with { CurrentChatRoom = generalChatRoom };
            return context;
        }
    }

}
