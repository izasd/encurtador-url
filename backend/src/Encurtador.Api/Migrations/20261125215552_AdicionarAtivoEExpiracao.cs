using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Encurtador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAtivoEExpiracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O EF gera defaultValue: false (o padrão de bool), o que desativaria todos os links
            // que já existem. Eles estavam no ar, então continuam ativos.
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Links",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiraEm",
                table: "Links",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "ExpiraEm",
                table: "Links");
        }
    }
}
