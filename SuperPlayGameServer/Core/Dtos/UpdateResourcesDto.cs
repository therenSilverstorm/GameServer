namespace SuperPlayGameServer.Core.Dtos
{
    public class UpdateResourcesDto
    {
        public string RecipientPlayerId { get; set; }
        public string ResourceType { get; set; }
        public int ResourceValue { get; set; }
    }
}
