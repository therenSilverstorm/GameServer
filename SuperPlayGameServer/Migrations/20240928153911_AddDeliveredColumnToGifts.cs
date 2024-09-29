using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPlayGameServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveredColumnToGifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Delivered",
                table: "Gifts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delivered",
                table: "Gifts");
        }
    }
}
