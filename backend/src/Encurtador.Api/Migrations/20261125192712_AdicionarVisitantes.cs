using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Encurtador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVisitantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EhVisitante",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EhVisitante",
                table: "Users");
        }
    }
}
