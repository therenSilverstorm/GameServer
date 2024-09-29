using System.ComponentModel.DataAnnotations;

namespace SuperPlayGameServer.Core.Entities
{
    public class PlayerState
    {
        [Key]
        public string PlayerId { get; set; }
        public string DeviceId { get; set; }
        public int Coins { get; set; }
        public int Rolls { get; set; }
        public bool IsLoggedIn { get; set; }

        //Currently resourceType hardcoded, for simplicity. Can use enums for better design,  or, in prod code, use a dictionary to store resources, as field. Example:
        //public Dictionary<string, Resource> Resources { get; set; } = new Dictionary<string, Resource>();



        public bool UpdateResource(string resourceType, int amount)
        {

            if (resourceType == "coins")
            {
                if (Coins + amount < 0) return false;
                Coins += amount;
            }

            else if (resourceType == "rolls")
            {
                if (Rolls + amount < 0) return false;
                Rolls += amount;
            }
            else
            {
                return false;
            }

            return true;
        }

       
        public int GetResource(string resourceType)
        {
            return resourceType switch
            {
                "coins" => Coins,
                "rolls" => Rolls,
                _ => 0  
            };
        }


    }
}
