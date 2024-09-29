using Serilog;
using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Dtos;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SuperPlayGameServer.WebSockets
{
    public class WebSocketMiddleware : CommandHandlerBase
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebSocketMiddleware(RequestDelegate next, IServiceProvider serviceProvider, JsonSerializerOptions jsonOptions)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _jsonOptions = jsonOptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Log.Information("WebSocket connection established at {Path}", context.Request.Path);

                    await HandleWebSocketConnection(context, webSocket);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during WebSocket handshake or connection at {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleWebSocketConnection(HttpContext context, WebSocket webSocket)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var correlationId = Guid.NewGuid().ToString();
            using var scope = _serviceProvider.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<WebSocketRouter>();
            var playerSessionManager = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            var buffer = new byte[1024 * 4];
            string playerId = null;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        playerId = await HandleTextMessage(result, buffer, webSocket, router, playerId);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleWebSocketClose(webSocket, playerId, playerSessionManager);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{CorrelationID}: Error processing WebSocket connection for player {PlayerId}", correlationId, playerId ?? "Unknown.");
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Error occurred", CancellationToken.None);
            }
        }

        private async Task<string> HandleTextMessage(WebSocketReceiveResult result, byte[] buffer, WebSocket webSocket, WebSocketRouter router, string currentPlayerId)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));
            if (router == null) throw new ArgumentNullException(nameof(router));

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
               
                // Deserialize the incoming JSON message into WebSocketMessage<JsonElement> with case-insensitive options
                var webSocketMessage = JsonSerializer.Deserialize<WebSocketMessage<JsonElement>>(message, _jsonOptions);

                if (webSocketMessage == null || string.IsNullOrEmpty(webSocketMessage.Command))
                {
                    await SendErrorMessage(webSocket, MessageType.Error, "Invalid message format");
                    Log.Warning("Received an invalid message format.");
                    return currentPlayerId;
                }

                // If the command is "Login", update the player ID based on the payload
                if (webSocketMessage.Command.Equals(nameof(CommandType.Login), StringComparison.OrdinalIgnoreCase))
                {
                    // Try to get the deviceId from the payload (which is a JsonElement)
                    if (webSocketMessage.Payload.TryGetProperty("deviceId", out JsonElement deviceIdElement))
                    {
                        currentPlayerId = deviceIdElement.GetString() ?? string.Empty;
                        Log.Information("Player {PlayerId} logged in", currentPlayerId);
                    }
                    else
                    {
                        Log.Warning("DeviceId not found in the payload.");
                    }
                }

                await router.RouteMessage(webSocketMessage.Command, webSocket, JsonSerializer.Serialize(webSocketMessage.Payload, _jsonOptions));
                return currentPlayerId;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Failed to parse WebSocket message.");
                await SendErrorMessage(webSocket, MessageType.Error, "Invalid JSON format");
            }

            return currentPlayerId;
        }


        private async Task HandleWebSocketClose(WebSocket webSocket, string playerId, IPlayerService playerSessionManager)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));
            if (playerSessionManager == null) throw new ArgumentNullException(nameof(playerSessionManager));

            Log.Information("WebSocket connection is closing.");

            if (!string.IsNullOrEmpty(playerId))
            {
                try
                {
                    // Reload the player's state to ensure we have the latest version
                    var playerState = await playerSessionManager.GetPlayerStateByIdAsync(playerId);

                    if (playerState != null && playerState.IsLoggedIn)
                    {
                        Log.Information("Player {PlayerId} is currently logged in, proceeding with logout.", playerId);

                        // Mark the player as logged out
                        playerState.IsLoggedIn = false;

                        // Update the player's state in the database
                        await playerSessionManager.UpdatePlayerStateAsync(playerState);

                        Log.Information("Player {PlayerId} marked as logged out.", playerId);
                    }
                    else
                    {
                        Log.Warning("Player {PlayerId} was not logged in or not found.", playerId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during logout for player {PlayerId}", playerId);
                }
            }

            // Ensure the connection is closed gracefully after logout is handled
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            Log.Information("WebSocket connection closed for player {PlayerId}", playerId ?? "Unknown");
        }
    }
}
