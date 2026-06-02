using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebInventory.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260602143000_AddInventoryApiToken")]
    public partial class AddInventoryApiToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApiTokenCreatedAt",
                table: "Inventories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiTokenHash",
                table: "Inventories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ApiTokenHash",
                table: "Inventories",
                column: "ApiTokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_ApiTokenHash",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ApiTokenCreatedAt",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ApiTokenHash",
                table: "Inventories");
        }
    }
}
