using Serilog;
using SuperPlayGameServer.Application.Common;
using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Dtos;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using System.Net.WebSockets;
using System.Text.Json;

namespace SuperPlayGameServer.Application.WebSocketHandlers;

[CommandHandler(CommandType.GetBalance)]
public class GetBalanceCommand : CommandHandlerBase, ICommand
{
    private readonly IPlayerService _sessionManager;
    private readonly JsonSerializerOptions _jsonOptions;

    public GetBalanceCommand(IPlayerService sessionManager, JsonSerializerOptions jsonOptions)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _jsonOptions = jsonOptions;
    }

    public async Task Execute(WebSocket webSocket, string messageContent)
    {
        if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            await SendErrorMessage(webSocket, MessageType.Error, "Invalid request. Player ID is required.");
            Log.Warning("GetBalanceCommand: Missing Player ID in the request.");
            return;
        }

        var getBalaneMessage = JsonSerializer.Deserialize<GetBalanceDto>(messageContent, _jsonOptions);
        Log.Information("Processing GetBalanceCommand for PlayerId: {PlayerId}", getBalaneMessage.PlayerId);

        try
        {
            var playerState = await _sessionManager.GetPlayerStateByIdAsync(getBalaneMessage.PlayerId);

            if (playerState != null)
            {
                Log.Information("Player {PlayerId} found. Coins: {Coins}, Rolls: {Rolls}", getBalaneMessage.PlayerId, playerState.Coins, playerState.Rolls);

                var balanceInfo = $"{playerState.PlayerId}::{playerState.Coins}::{playerState.Rolls}";
                await SendSuccessMessage(webSocket, MessageType.BalanceInfo, balanceInfo);
            }
            else
            {
                Log.Warning("Player {PlayerId} not found.", getBalaneMessage.PlayerId);
                await SendErrorMessage(webSocket, MessageType.PlayerNotFound, "Player not found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while processing GetBalanceCommand for PlayerId: {PlayerId}", getBalaneMessage.PlayerId);
            await SendErrorMessage(webSocket, MessageType.Error, "An error occurred while retrieving the balance.");
        }
    }
}

