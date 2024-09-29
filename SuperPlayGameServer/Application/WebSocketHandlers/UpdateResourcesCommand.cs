using Serilog;
using SuperPlayGameServer.Application.Common;
using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Dtos;
using SuperPlayGameServer.Core.Entities;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using System.Net.WebSockets;
using System.Text.Json;

namespace SuperPlayGameServer.Application.WebSocketHandlers;

[CommandHandler(CommandType.UpdateResources)]
public class UpdateResourcesCommand : CommandHandlerBase, ICommand
{
    private readonly IPlayerService _sessionManager;
    private readonly JsonSerializerOptions _jsonOptions;

    public UpdateResourcesCommand(IPlayerService sessionManager, JsonSerializerOptions jsonOptions)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _jsonOptions = jsonOptions;
    }

    public async Task Execute(WebSocket webSocket, string messageContent)
    {
        if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

        try
        {
            // Deserialize the incoming message content to UpdateResourcesDto
            var updateResourcesMessage = JsonSerializer.Deserialize<UpdateResourcesDto>(messageContent, _jsonOptions);

            // Validate the deserialized message
            if (updateResourcesMessage == null || string.IsNullOrWhiteSpace(updateResourcesMessage.RecipientPlayerId) ||
                string.IsNullOrWhiteSpace(updateResourcesMessage.ResourceType) || updateResourcesMessage.ResourceValue <= 0)
            {
                await HandleInvalidMessageContent(webSocket);
                return;
            }

            var playerId = updateResourcesMessage.RecipientPlayerId.Trim();
            var resourceType = updateResourcesMessage.ResourceType.Trim();
            var resourceValue = updateResourcesMessage.ResourceValue;

            // Process the resource update
            await ProcessResourceUpdate(webSocket, playerId, resourceType, resourceValue);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Failed to parse WebSocket message.");
            await SendErrorMessage(webSocket, MessageType.Error, "Invalid JSON format");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during resource update.");
            await SendErrorMessage(webSocket, MessageType.Error, "An error occurred during resource update.");
        }
    }

    private async Task ProcessResourceUpdate(WebSocket webSocket, string playerId, string resourceType, int resourceValue)
    {
        try
        {
            var playerState = await _sessionManager.GetPlayerStateByIdAsync(playerId);
            if (playerState == null)
            {
                await HandlePlayerNotFound(webSocket, playerId);
                return;
            }

            if (playerState.UpdateResource(resourceType, resourceValue))
            {
                await HandleResourceUpdateSuccess(webSocket, playerId, resourceType, playerState);
            }
            else
            {
                await HandleInvalidResourceUpdate(webSocket, resourceType, playerId);
            }
        }
        catch (Exception ex)
        {
            await HandleException(webSocket, playerId, ex);
        }
    }

    private async Task HandlePlayerNotFound(WebSocket webSocket, string playerId)
    {
        const string errorMessage = "Player not found.";
        await SendErrorMessage(webSocket, MessageType.PlayerNotFound, errorMessage);
        Log.Warning("Player not found for PlayerId: {PlayerId}", playerId);
    }

    private async Task HandleResourceUpdateSuccess(WebSocket webSocket, string playerId, string resourceType, PlayerState playerState)
    {
        await _sessionManager.UpdatePlayerStateAsync(playerState);
        var successMessage = $"{resourceType}::{playerState.GetResource(resourceType)}";
        await SendSuccessMessage(webSocket, MessageType.UpdateSuccess, successMessage);
        Log.Information("Player {PlayerId} resource {ResourceType} updated to {ResourceValue}.", playerId, resourceType, playerState.GetResource(resourceType));
    }

    private async Task HandleInvalidResourceUpdate(WebSocket webSocket, string resourceType, string playerId)
    {
        const string errorMessage = "Invalid resource update.";
        await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
        Log.Warning("Failed to update resource {ResourceType} for PlayerId: {PlayerId} due to invalid operation.", resourceType, playerId);
    }

    private async Task HandleException(WebSocket webSocket, string playerId, Exception ex)
    {
        const string errorMessage = "An error occurred while processing the update request.";
        await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
        Log.Error(ex, "Error occurred while updating resources for PlayerId: {PlayerId}", playerId);
    }

    private async Task HandleInvalidMessageContent(WebSocket webSocket)
    {
        const string errorMessage = "Invalid message content.";
        await SendErrorMessage(webSocket, MessageType.Error, errorMessage);
        Log.Warning("Received invalid UpdateResources message content.");
    }
}
