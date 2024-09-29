using Microsoft.EntityFrameworkCore;
using SuperPlayGameServer.Core.Entities;
using SuperPlaySuperPlayGameServer.Core.Entities;

namespace SuperPlayGameServer.Infra.Data
{
    public class GameDbContext : DbContext
    {
        public DbSet<PlayerState> PlayerStates { get; set; }
        public DbSet<Gift> Gifts { get; set; }

        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerState>()
                .HasKey(p => p.PlayerId);

            modelBuilder.Entity<PlayerState>()
                .HasIndex(p => p.DeviceId)
                .IsUnique();

            modelBuilder.Entity<Gift>()
                .HasKey(g => g.Id);

            modelBuilder.Entity<Gift>()
                .HasIndex(g => new { g.SenderPlayerId, g.RecipientPlayerId });
        }
    }
}
