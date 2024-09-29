using SuperPlayGameServer.Core.Entities;

namespace SuperPlayGameServer.Core.Interfaces
{
    public interface IPlayerRepository
    {
        Task<PlayerState> GetPlayerByIdAsync(string playerId);
        Task<PlayerState> GetPlayerByDeviceIdAsync(string deviceId);  
        Task SavePlayerAsync(PlayerState player); 
        Task<IEnumerable<PlayerState>> GetAllLoggedInPlayersAsync();
    }
}
