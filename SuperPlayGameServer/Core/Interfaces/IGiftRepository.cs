using SuperPlaySuperPlayGameServer.Core.Entities;

namespace SuperPlayGameServer.Core.Interfaces
{
    public interface IGiftRepository
    {
        Task QueueGiftAsync(Gift gift);
        Task<IEnumerable<Gift>> GetQueuedGiftsForPlayerAsync(string playerId);
        Task UpdateGiftAsync(Gift gift);
    }
}
