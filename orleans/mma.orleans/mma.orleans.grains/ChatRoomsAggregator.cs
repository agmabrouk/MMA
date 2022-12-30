using mma.orleans.interfaces;
using Orleans;

namespace mma.orleans.grains
{
    public class ChatRoomsAggregator : Grain, IChatRoomsAggregator
    {
        private List<string> _availableChatRooms = new List<string>();
        public Task<bool> AddNewChatRoom(string roomName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetAvailableChatRooms()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveChatRoom(string chatRoomName)
        {
            throw new NotImplementedException();
        }
    }
}
