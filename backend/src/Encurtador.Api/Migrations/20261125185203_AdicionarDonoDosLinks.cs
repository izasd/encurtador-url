using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Encurtador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDonoDosLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sem valor padrão de propósito: todo link precisa de um dono real. Num banco que já
            // tenha links (criados antes do login), a migration falha em vez de inventar um dono.
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Links",
                type: "integer",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Links_UserId",
                table: "Links",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Users_UserId",
                table: "Links",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_Users_UserId",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_Links_UserId",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Links");
        }
    }
}
