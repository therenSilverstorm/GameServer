using System.ComponentModel.DataAnnotations;

namespace SuperPlaySuperPlayGameServer.Core.Entities
{
    public class Gift
    {
        [Key]
        public int Id { get; set; }
        public string ResourceType { get; set; }
        public int ResourceValue { get; set; }
        public string SenderPlayerId { get; set; }
        public string RecipientPlayerId { get; set; }
        public bool Delivered { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
