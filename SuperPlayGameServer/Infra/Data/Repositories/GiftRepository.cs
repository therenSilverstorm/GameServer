using Microsoft.EntityFrameworkCore;
using SuperPlayGameServer.Core.Interfaces;
using SuperPlaySuperPlayGameServer.Core.Entities;

namespace SuperPlayGameServer.Infra.Data.Repositories
{
    public class GiftRepository : IGiftRepository
    {
        private readonly GameDbContext _context;

        public GiftRepository(GameDbContext context)
        {
            _context = context;
        }

        // Queue a new gift for later delivery
        public async Task QueueGiftAsync(Gift gift)
        {
            if (gift == null) throw new ArgumentNullException(nameof(gift));

            _context.Gifts.Add(gift);
            await _context.SaveChangesAsync();
        }

        // Get all queued (undelivered) gifts for a specific player
        public async Task<IEnumerable<Gift>> GetQueuedGiftsForPlayerAsync(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentNullException(nameof(playerId));

            return await _context.Gifts
                .Where(g => g.RecipientPlayerId == playerId && !g.Delivered)
                .ToListAsync();
        }

        // Update a gift, mark as delivered
        public async Task UpdateGiftAsync(Gift gift)
        {
            if (gift == null) throw new ArgumentNullException(nameof(gift));

            _context.Gifts.Update(gift);
            await _context.SaveChangesAsync();
        }
    }
}
