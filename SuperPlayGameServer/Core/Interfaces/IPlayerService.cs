using Microsoft.EntityFrameworkCore.Storage;
using SuperPlayGameServer.Core.Entities;

namespace SuperPlayGameServer.Core.Interfaces
{
    public interface IPlayerService
    {
        Task<PlayerState> GetPlayerStateByDeviceIdAsync(string deviceId);
        Task<PlayerState> GetPlayerStateByIdAsync(string playerId);
        Task AddPlayerAsync(PlayerState playerState);
        Task UpdatePlayerStateAsync(PlayerState playerState);
        string GeneratePlayerId(string deviceId);
        Task<IEnumerable<PlayerState>> GetAllLoggedInPlayersAsync();
    }
}
