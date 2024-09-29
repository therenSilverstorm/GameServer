
using System.Net.WebSockets;

namespace SuperPlayGameServer.Core.Interfaces
{
    public interface ICommand
    {
        Task Execute(WebSocket webSocket, string messageContent);
    }
}
