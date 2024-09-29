namespace SuperPlayGameServer.Core.Dtos
{
    public class SendGiftDto
    {
        public string SenderPlayerId { get; set; }
        public string RecipientPlayerId { get; set; }
        public string ResourceType { get; set; }
        public int ResourceValue { get; set; }
    }
}
