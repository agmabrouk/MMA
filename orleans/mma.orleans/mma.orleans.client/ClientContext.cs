using Orleans;

namespace mma.orleans.client
{
    public readonly record struct ClientContext(
      IClusterClient Client,
      string? UserName = null,
      string? CurrentChatRoom = null);
}
