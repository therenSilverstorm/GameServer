using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPlayGameServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceType = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceValue = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderPlayerId = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientPlayerId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStates",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    Coins = table.Column<int>(type: "INTEGER", nullable: false),
                    Rolls = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLoggedIn = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStates", x => x.PlayerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_SenderPlayerId_RecipientPlayerId",
                table: "Gifts",
                columns: new[] { "SenderPlayerId", "RecipientPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStates_DeviceId",
                table: "PlayerStates",
                column: "DeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gifts");

            migrationBuilder.DropTable(
                name: "PlayerStates");
        }
    }
}
