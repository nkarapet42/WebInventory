using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebInventory.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260602135000_AddSalesforceUserLink")]
    public partial class AddSalesforceUserLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesforceAccountId",
                table: "AspNetUsers",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesforceContactId",
                table: "AspNetUsers",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesforceSyncedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SalesforceAccountId",
                table: "AspNetUsers",
                column: "SalesforceAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SalesforceContactId",
                table: "AspNetUsers",
                column: "SalesforceContactId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SalesforceAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SalesforceContactId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SalesforceAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SalesforceContactId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SalesforceSyncedAt",
                table: "AspNetUsers");
        }
    }
}
