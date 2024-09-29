using Microsoft.EntityFrameworkCore;
using SuperPlayGameServer.Core.Entities;
using SuperPlayGameServer.Core.Interfaces;

namespace SuperPlayGameServer.Infra.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly GameDbContext _context;

    public PlayerRepository(GameDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlayerState> GetPlayerByIdAsync(string playerId)
    {
        return await _context.PlayerStates.AsNoTracking().SingleOrDefaultAsync(p => p.PlayerId == playerId);
    }

    public async Task<PlayerState> GetPlayerByDeviceIdAsync(string deviceId)
    {
        return await _context.PlayerStates.AsNoTracking().SingleOrDefaultAsync(p => p.DeviceId == deviceId);
    }

    public async Task SavePlayerAsync(PlayerState player)
    {
        var existingPlayer = await _context.PlayerStates
                                           .SingleOrDefaultAsync(p => p.PlayerId == player.PlayerId);
        if (existingPlayer != null)
        {
            existingPlayer.Coins = player.Coins;
            existingPlayer.Rolls = player.Rolls;
            existingPlayer.IsLoggedIn = player.IsLoggedIn;
            _context.PlayerStates.Update(existingPlayer);
        }
        else
        {
            await _context.PlayerStates.AddAsync(player);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PlayerState>> GetAllLoggedInPlayersAsync()
    {
        return await _context.PlayerStates
            .AsNoTracking()
            .Where(p => p.IsLoggedIn)
            .ToListAsync();
    }
}
