namespace SuperPlayGameServer.Core.Dtos
{
    public class WebSocketMessage<TPayload>
    {
        public string Command { get; set; }
        public TPayload Payload { get; set; }
    }

}
