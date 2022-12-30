using Orleans.Runtime;
using Orleans.Streams;

namespace mma.orleans
{
    // [GrainDirectory(GrainDirectoryName = "my-grain-directory")]
    public class ChatRoomGrain : Grain, IChatRoomGrain
    {
        private readonly List<ChatMsg> _messages = new(100);
        private readonly List<string> _users = new(100);
        private string _chatRommName { get; set; }
        private IAsyncStream<ChatMsg> _stream = null!;

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _chatRommName = this.GetPrimaryKeyString();
            var streamProvider = this.GetStreamProvider("MMAChat");
            var streamId = StreamId.Create("ChatRoom", this.GetPrimaryKeyString());
            _stream = streamProvider.GetStream<ChatMsg>(streamId);
            return base.OnActivateAsync(cancellationToken);
        }


        public async Task<StreamId> Join(string nickname)
        {
            _users.Add(nickname);

            await _stream.OnNextAsync(
                new ChatMsg("System", $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ..."));

            return _stream.StreamId;
        }

        public async Task<StreamId> Leave(string nickname)
        {
            _users.Remove(nickname);
            await _stream.OnNextAsync(
                 new ChatMsg("System", $"{nickname} leaves the chat..."));

            return _stream.StreamId;
        }


        public async Task<bool> SendMessage(ChatMsg message)
        {
            _messages.Add(message);
            await _stream.OnNextAsync(message);

            return true;
        }

        public Task<string> GetChatRoom()
        {
            return Task.FromResult(_chatRommName);
        }

        public Task<ChatMsg[]> ReadHistory(int numberOfMessages)
        {
            var response = _messages
            .OrderByDescending(x => x.Created)
            .Take(numberOfMessages)
            .OrderBy(x => x.Created)
            .ToArray();

            return Task.FromResult(response);
        }

        public Task<string[]> GetMembers() => Task.FromResult(_users.ToArray());
    }

}
