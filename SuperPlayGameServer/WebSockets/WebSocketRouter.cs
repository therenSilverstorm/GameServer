using SuperPlayGameServer.Application.Factory;
using SuperPlayGameServer.Core.Enums;
using System.Net.WebSockets;

namespace SuperPlayGameServer.WebSockets
{
    public class WebSocketRouter : CommandHandlerBase
    {
        private readonly ICommandHandlerFactory _commandHandlerFactory;

        public WebSocketRouter(ICommandHandlerFactory commandHandlerFactory)
        {
            _commandHandlerFactory = commandHandlerFactory;
        }


        public async Task RouteMessage(string commandType, WebSocket webSocket, dynamic payload)
        {
            if (Enum.TryParse<CommandType>(commandType, true, out var command))
            {
                var handler = _commandHandlerFactory.ResolveCommandHandler(command);
                if (handler != null)
                {
                    // Pass the deserialized payload to the handler
                    await handler.Execute(webSocket, payload.ToString());
                }
                else
                {
                    await SendErrorMessage(webSocket, MessageType.UnknownCommand, commandType);
                }
            }
            else
            {
                await SendErrorMessage(webSocket, MessageType.UnknownCommand, commandType);
            }
        }

    }
}
