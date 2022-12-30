using Orleans;

namespace mma.orleans
{
    public interface IChatRoomsAggregator : IGrainWithStringKey
    {
        Task<bool> AddNewChatRoom(string roomName);
        Task<string> GetAvailableChatRooms();
        Task<bool> RemoveChatRoom(string chatRoomName);
    }
}
