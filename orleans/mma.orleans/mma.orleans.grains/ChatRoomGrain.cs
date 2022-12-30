using mma.orleans.interfaces;
using mma.orleans.types;
using Orleans;
using Orleans.GrainDirectory;
using Orleans.Runtime;
using Orleans.Streams;

namespace mma.orleans.grains
{
    [GrainDirectory(GrainDirectoryName = "my-grain-directory")]
    public class ChatRoomGrain : Grain, IChatRoomGrain
    {
        private ChatRoom _chatRoom;
        private IAsyncStream<ChatMessage> _stream = null!;


        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _chatRoom = new ChatRoom
            {
                Name = this.GetPrimaryKeyString(),
                Users = new List<string>(),
                Messages = new List<ChatMessage>()
            };

            var streamProvider = this.GetStreamProvider("MMAChat");
            var streamId = StreamId.Create("ChatRoom", this.GetPrimaryKeyString());
            _stream = streamProvider.GetStream<ChatMessage>(streamId);
            return base.OnActivateAsync(cancellationToken);
        }


        public async Task<StreamId> Join(string nickname)
        {
            _chatRoom.Users.Add(nickname);

            await _stream.OnNextAsync(
                new ChatMessage
                {
                    User = "System",
                    Text = $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ..."
                });

            return _stream.StreamId;
        }

        public async Task<StreamId> Leave(string nickname)
        {
            _chatRoom.Users.Remove(nickname);
            await _stream.OnNextAsync(
                 new ChatMessage
                 {
                     User = "System",
                     Text = $"{nickname} leaves the chat..."
                 });

            return _stream.StreamId;
        }

  
        public async Task<bool> SendMessage(ChatMessage message)
        {
            _chatRoom.Messages.Add(message);
            await _stream.OnNextAsync(message);

            return true;
        }

        public Task<ChatRoom> GetChatRoom()
        {
            return Task.FromResult(_chatRoom);
        }

        public Task<ChatMessage[]> ReadHistory(int numberOfMessages)
        {
            var response = _chatRoom.Messages
            .OrderByDescending(x => x.Timestamp)
            .Take(numberOfMessages)
            .OrderBy(x => x.Timestamp)
            .ToArray();

            return Task.FromResult(response);
        }

        public Task<string[]> GetMembers() => Task.FromResult(_chatRoom.Users.ToArray());
    }

}
