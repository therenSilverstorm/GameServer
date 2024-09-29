using SuperPlayGameServer.Core.Enums;

namespace SuperPlayGameServer.Application.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandHandlerAttribute : Attribute
    {
        public CommandType CommandType { get; }

        public CommandHandlerAttribute(CommandType commandType)
        {
            CommandType = commandType;
        }
    }
}
