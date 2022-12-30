using Orleans.Runtime;

namespace mma.orleans
{
    public interface IChatRoomGrain : IGrainWithStringKey
    {
        Task<StreamId> Join(string nickname);
        Task<StreamId> Leave(string nickname);
        Task<bool> SendMessage(ChatMsg msg);
        Task<string> GetChatRoom();
        Task<ChatMsg[]> ReadHistory(int numberOfMessages);
        Task<string[]> GetMembers();
    }
}