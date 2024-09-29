using SuperPlayGameServer.Application.Common;
using SuperPlayGameServer.Core.Enums;
using SuperPlayGameServer.Core.Interfaces;
using System.Reflection;

namespace SuperPlayGameServer.Application.Factory;

public class CommandHandlerFactory : ICommandHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<CommandType, Type> _commandMap;

    public CommandHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _commandMap = DiscoverCommandHandlers();
    }

    private Dictionary<CommandType, Type> DiscoverCommandHandlers()
    {
        var commandHandlers = new Dictionary<CommandType, Type>();

        var assembly = Assembly.GetExecutingAssembly();
        var commandTypes = assembly.GetTypes()
                                   .Where(t => t.GetInterfaces().Contains(typeof(ICommand)));

        foreach (var type in commandTypes)
        {
            var attribute = type.GetCustomAttribute<CommandHandlerAttribute>();
            if (attribute != null)
            {
                commandHandlers[attribute.CommandType] = type;
            }
        }

        return commandHandlers;
    }

    public ICommand ResolveCommandHandler(CommandType commandType)
    {
        if (_commandMap.TryGetValue(commandType, out var handlerType))
        {
            return _serviceProvider.GetService(handlerType) as ICommand;
        }
        return null;
    }
}

