using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;

namespace SuperPlayGameServer.Application.Factory;
public interface ICommandHandlerFactory
{
    ICommand ResolveCommandHandler(CommandType commandType);
}