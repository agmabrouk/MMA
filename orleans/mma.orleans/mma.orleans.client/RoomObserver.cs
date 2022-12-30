using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace mma.orleans.client
{
    public sealed class ChatMessageObserver : IAsyncObserver<ChatMsg>
    {
        private readonly string _roomName;

        public ChatMessageObserver(string roomName) => _roomName = roomName;

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex)
        {
            Console.Error.WriteLine(ex);

            return Task.CompletedTask;
        }

        public Task OnNextAsync(ChatMsg item, StreamSequenceToken? token = null)
        {
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine("[{0}][{1}][{2}]:{3}", item.Created.LocalDateTime.ToShortTimeString(), _roomName, item.Author, item.Text);
            Console.WriteLine("--------------------------------------------------------------------");
            return Task.CompletedTask;
        }
    }

}
