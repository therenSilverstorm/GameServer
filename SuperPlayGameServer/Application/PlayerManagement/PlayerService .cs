using Serilog;
using SuperPlayGameServer.Core.Entities;
using SuperPlayGameServer.Core.Interfaces;

namespace SuperPlayGameServer.Application.PlayerManagement
{

    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;

        public PlayerService(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        }

        public async Task<PlayerState> GetPlayerStateByDeviceIdAsync(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException("DeviceId cannot be null or empty.", nameof(deviceId));
            }

            return await _playerRepository.GetPlayerByDeviceIdAsync(deviceId);
        }

        public async Task<PlayerState> GetPlayerStateByIdAsync(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("PlayerId cannot be null or empty.", nameof(playerId));
            }

            return await _playerRepository.GetPlayerByIdAsync(playerId);
        }

        public async Task AddPlayerAsync(PlayerState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            await _playerRepository.SavePlayerAsync(playerState);
        }

        public async Task UpdatePlayerStateAsync(PlayerState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            await _playerRepository.SavePlayerAsync(playerState);
        }

        // For production, consider using GUID or a more secure method
        public string GeneratePlayerId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                Log.Warning("GeneratePlayerId: DeviceId is null or empty.");
                throw new ArgumentException("DeviceId cannot be null or empty.", nameof(deviceId));
            }

            Log.Information("Generating PlayerId based on DeviceId: {DeviceId}", deviceId);
            return deviceId;

            // Example: Generating a secure hash using SHA256 (commented out)
            /*
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(deviceId));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
            */
        }

        public async Task<IEnumerable<PlayerState>> GetAllLoggedInPlayersAsync()
        {
            return await _playerRepository.GetAllLoggedInPlayersAsync();
        }
    }

}
