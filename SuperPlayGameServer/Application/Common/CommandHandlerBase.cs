using SuperPlayGameServer.Core.Enums;
using System.Net.WebSockets;
using System.Text;

namespace SuperPlayGameServer.Application.Factory
{
    public abstract class CommandHandlerBase
    {
        protected async Task SendSuccessMessage(WebSocket webSocket, MessageType messageType, string messageContent)
        {
            var successMessage = Encoding.UTF8.GetBytes($"{messageType}::{messageContent}");
            await webSocket.SendAsync(new ArraySegment<byte>(successMessage, 0, successMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        protected async Task SendErrorMessage(WebSocket webSocket, MessageType messageType, string errorMessage)
        {
            var errorBytes = Encoding.UTF8.GetBytes($"{messageType}::{errorMessage}");
            await webSocket.SendAsync(new ArraySegment<byte>(errorBytes, 0, errorBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }





}
