using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebInventory.Infrastructure.Data.Migrations
{
    public partial class UseXminRowVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Items",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Inventories",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());
        }
    }
}
