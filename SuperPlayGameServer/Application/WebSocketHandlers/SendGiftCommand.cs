using Serilog;
using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using SuperPlaySuperPlayGameServer.Core.Entities;
using System.Net.WebSockets;
using SuperPlayGameServer.Infra.Data;
using SuperPlayGameServer.Application.Common;
using System.Text.Json;
using SuperPlayGameServer.Core.Dtos; 

namespace SuperPlayGameServer.Application.WebSocketHandlers
{
    [CommandHandler(CommandType.SendGift)]
    public class SendGiftCommand : CommandHandlerBase, ICommand
    {
        private readonly IPlayerService _sessionManager;
        private readonly IGiftRepository _giftRepository;
        private readonly GameDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;

        public SendGiftCommand(IPlayerService sessionManager, IGiftRepository giftRepository, GameDbContext context, JsonSerializerOptions jsonOptions)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _giftRepository = giftRepository ?? throw new ArgumentNullException(nameof(giftRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jsonOptions = jsonOptions;
        }

        public async Task Execute(WebSocket webSocket, string messageContent)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            try
            {
                // Deserialize the incoming message content to SendGiftDto
                var sendGiftMessage = JsonSerializer.Deserialize<SendGiftDto>(messageContent, _jsonOptions);

                if (sendGiftMessage == null || string.IsNullOrWhiteSpace(sendGiftMessage.SenderPlayerId) ||
                    string.IsNullOrWhiteSpace(sendGiftMessage.RecipientPlayerId) || sendGiftMessage.ResourceValue <= 0)
                {
                    await HandleInvalidMessageContent(webSocket);
                    return;
                }

                await ProcessGiftTransaction(webSocket, sendGiftMessage.SenderPlayerId, sendGiftMessage.RecipientPlayerId, sendGiftMessage.ResourceType, sendGiftMessage.ResourceValue);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Failed to parse WebSocket message.");
                await SendErrorMessage(webSocket, MessageType.Error, "Invalid JSON format");
            }
            catch (Exception ex)
            {
                await HandleException(webSocket, ex);
            }
        }

        private async Task ProcessGiftTransaction(WebSocket webSocket, string senderPlayerId, string recipientPlayerId, string resourceType, int resourceValue)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var senderState = await _sessionManager.GetPlayerStateByIdAsync(senderPlayerId);
                var recipientState = await _sessionManager.GetPlayerStateByIdAsync(recipientPlayerId);

                if (senderState == null || recipientState == null)
                {
                    await HandleSenderOrRecipientNotFound(webSocket, senderPlayerId, recipientPlayerId);
                    return;
                }

                if (!senderState.UpdateResource(resourceType, -resourceValue))
                {
                    await HandleInsufficientResources(webSocket, senderPlayerId, resourceType);
                    return;
                }

                await _sessionManager.UpdatePlayerStateAsync(senderState);


                if (!recipientState.IsLoggedIn)
                {

                    await QueueGiftForOfflineRecipient(senderPlayerId, recipientPlayerId, resourceType, resourceValue);
                    await NotifyGiftQueued(webSocket, senderPlayerId, recipientPlayerId, resourceType, resourceValue);


                    await transaction.CommitAsync();
                    return;
                }

                recipientState.UpdateResource(resourceType, resourceValue);

                await _sessionManager.UpdatePlayerStateAsync(recipientState);

                await RecordGiftTransaction(senderPlayerId, recipientPlayerId, resourceType, resourceValue);

                await NotifyGiftSuccess(webSocket, senderPlayerId, recipientPlayerId, resourceType, resourceValue);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        private async Task QueueGiftForOfflineRecipient(string senderPlayerId, string recipientPlayerId, string resourceType, int resourceValue)
        {
            var gift = new Gift
            {
                SenderPlayerId = senderPlayerId,
                RecipientPlayerId = recipientPlayerId,
                ResourceType = resourceType,
                ResourceValue = resourceValue,
                CreatedAt = DateTime.UtcNow,
                Delivered = false
            };

            await _giftRepository.QueueGiftAsync(gift);
            Log.Information("Gift from {SenderPlayerId} to {RecipientPlayerId} queued for later delivery.", senderPlayerId, recipientPlayerId);
        }

        private async Task RecordGiftTransaction(string senderPlayerId, string recipientPlayerId, string resourceType, int resourceValue)
        {
            var gift = new Gift
            {
                SenderPlayerId = senderPlayerId,
                RecipientPlayerId = recipientPlayerId,
                ResourceType = resourceType,
                ResourceValue = resourceValue,
                CreatedAt = DateTime.UtcNow
            };

            await _giftRepository.QueueGiftAsync(gift);
            Log.Information("Gift sent from {SenderPlayerId} to {RecipientPlayerId}. Resource: {ResourceType}, Value: {ResourceValue}.",
                senderPlayerId, recipientPlayerId, resourceType, resourceValue);
        }

        private async Task NotifyGiftQueued(WebSocket webSocket, string senderPlayerId, string recipientPlayerId, string resourceType, int resourceValue)
        {
            // This message informs the sender that the gift has been successfully queued or delivered
            var queuedMessage = $"{senderPlayerId}::{recipientPlayerId}::{resourceType}::{resourceValue}::Queued";
            await SendSuccessMessage(webSocket, MessageType.GiftQueued, queuedMessage);

            Log.Information("Gift from {SenderPlayerId} to {RecipientPlayerId} has been queued for delivery. Resource: {ResourceType}, Value: {ResourceValue}.",
                senderPlayerId, recipientPlayerId, resourceType, resourceValue);
        }

        private async Task NotifyGiftSuccess(WebSocket webSocket, string senderPlayerId, string recipientPlayerId, string resourceType, int resourceValue)
        {
            var successMessage = $"{senderPlayerId}::{recipientPlayerId}::{resourceType}::{resourceValue}";
            await SendSuccessMessage(webSocket, MessageType.GiftSuccess, successMessage);
        }

        private async Task HandleException(WebSocket webSocket, Exception ex)
        {
            const string errorMessage = "An error occurred while processing your request.";
            await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
            Log.Error(ex, "Error occurred while processing SendGiftCommand.");
        }

        private async Task HandleInvalidMessageContent(WebSocket webSocket)
        {
            const string errorMessage = "Invalid message content.";
            await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
            Log.Warning("Received empty message content in SendGiftCommand.");
        }

        private async Task HandleSenderOrRecipientNotFound(WebSocket webSocket, string senderPlayerId, string recipientPlayerId)
        {
            const string errorMessage = "Sender or recipient not found.";
            await SendErrorMessage(webSocket, MessageType.PlayerNotFound, errorMessage);
            Log.Warning("Either sender {SenderPlayerId} or recipient {RecipientPlayerId} not found.", senderPlayerId, recipientPlayerId);
        }

        private async Task HandleInsufficientResources(WebSocket webSocket, string senderPlayerId, string resourceType)
        {
            const string errorMessage = "Insufficient resources.";
            await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
            Log.Warning("Sender {SenderPlayerId} has insufficient {ResourceType}.", senderPlayerId, resourceType);
        }
    }
}
