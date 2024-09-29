using Serilog;
using SuperPlayGameServer.Application.Common;
using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Dtos;
using SuperPlayGameServer.Core.Entities;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using System.Net.WebSockets;
using System.Text.Json;

namespace SuperPlayGameServer.Application.WebSocketHandlers
{
    [CommandHandler(CommandType.Login)]
    public class LoginCommand : CommandHandlerBase, ICommand
    {
        private readonly IPlayerService _sessionManager;
        private readonly IGiftRepository _giftRepository;
        private readonly JsonSerializerOptions _jsonOptions;

        public LoginCommand(IPlayerService sessionManager, IGiftRepository giftRepository, JsonSerializerOptions jsonOptions)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _giftRepository = giftRepository;
            _jsonOptions = jsonOptions;
        }

        public async Task Execute(WebSocket webSocket, string messageContent)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            try
            {
                // Deserialize the incoming message content
                var loginMessage = JsonSerializer.Deserialize<LoginDto>(messageContent, _jsonOptions);

                if (loginMessage == null || string.IsNullOrWhiteSpace(loginMessage.DeviceId))
                {
                    await HandleInvalidLoginRequest(webSocket);
                    return;
                }

                var deviceId = loginMessage.DeviceId.Trim();
                await HandlePlayerState(webSocket, deviceId);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Failed to parse WebSocket message.");
                await SendErrorMessage(webSocket, MessageType.Error, "Invalid JSON format");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during login.");
                await SendErrorMessage(webSocket, MessageType.LoginFailed, "An error occurred during login.");
            }
        }

        private async Task HandlePlayerState(WebSocket webSocket, string deviceId)
        {
            var playerState = await _sessionManager.GetPlayerStateByDeviceIdAsync(deviceId);

            if (playerState != null && playerState.IsLoggedIn)
            {
                await SendErrorMessage(webSocket, MessageType.LoginFailed, "PlayerAlreadyLoggedIn");
                Log.Warning("Player with DeviceId {DeviceId} is already logged in.", deviceId);
                return;
            }

            if (playerState == null)
            {
                await CreateNewPlayer(webSocket, deviceId);
            }
            else
            {
                await LoginExistingPlayer(webSocket, playerState);
            }
        }

        private async Task CreateNewPlayer(WebSocket webSocket, string deviceId)
        {
            var playerId = _sessionManager.GeneratePlayerId(deviceId);
            var newPlayerState = InitiateUser(deviceId, playerId);

            await _sessionManager.AddPlayerAsync(newPlayerState);
            Log.Information("New player created with PlayerId: {PlayerId}", playerId);

            await SendSuccessMessage(webSocket, MessageType.LoginSuccess, $"{newPlayerState.PlayerId}");
        }


        private async Task LoginExistingPlayer(WebSocket webSocket, PlayerState playerState)
        {
            playerState.IsLoggedIn = true;
            await _sessionManager.UpdatePlayerStateAsync(playerState);
            Log.Information("Player {PlayerId} logged in successfully.", playerState.PlayerId);

            await CheckAndDeliverUndeliveredGifts(webSocket, playerState.PlayerId);

            await SendSuccessMessage(webSocket, MessageType.LoginSuccess, $"{playerState.PlayerId}");
        }


        private async Task CheckAndDeliverUndeliveredGifts(WebSocket webSocket, string playerId)
        {
            var undeliveredGifts = await _giftRepository.GetQueuedGiftsForPlayerAsync(playerId);

            if (undeliveredGifts == null || !undeliveredGifts.Any())
            {
                Log.Information("No undelivered gifts for player {PlayerId}", playerId);
                return;
            }

            foreach (var gift in undeliveredGifts)
            {
                var recipientState = await _sessionManager.GetPlayerStateByIdAsync(playerId);
                if (recipientState != null)
                {
                    recipientState.UpdateResource(gift.ResourceType, gift.ResourceValue);
                    await _sessionManager.UpdatePlayerStateAsync(recipientState);

                    gift.Delivered = true;
                    await _giftRepository.UpdateGiftAsync(gift);

                    var giftDeliveredMessage = $"GiftDelivered::{gift.SenderPlayerId}::{gift.ResourceType}::{gift.ResourceValue}";
                    await SendSuccessMessage(webSocket, MessageType.GiftDelivered, giftDeliveredMessage);

                    Log.Information("Delivered gift from {SenderPlayerId} to {RecipientPlayerId}. Resource: {ResourceType}, Value: {ResourceValue}.",
                        gift.SenderPlayerId, playerId, gift.ResourceType, gift.ResourceValue);
                }
            }
        }
        private async Task HandleInvalidLoginRequest(WebSocket webSocket)
        {
            const string errorMessage = "Invalid login request. DeviceId is required.";
            await SendErrorMessage(webSocket, MessageType.LoginFailed, errorMessage);
            Log.Warning("Login request failed due to missing DeviceId.");
        }

        private static PlayerState InitiateUser(string deviceId, string playerId)
        {
            return new PlayerState
            {
                PlayerId = playerId,
                DeviceId = deviceId,
                Coins = 100,
                Rolls = 10,
                IsLoggedIn = true
            };
        }
    }

}

