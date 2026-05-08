using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace WebInventory.Infrastructure.Data.Migrations
{
    public partial class AddSearchVectors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Items",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple', coalesce(\"CustomId\", '') || ' ' || coalesce(\"Text1\", '') || ' ' || coalesce(\"Text2\", '') || ' ' || coalesce(\"Text3\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Inventories",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"DescriptionMarkdown\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Tags",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple', coalesce(\"Name\", ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_SearchVector",
                table: "Items",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_SearchVector",
                table: "Inventories",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_SearchVector",
                table: "Tags",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_SearchVector",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_SearchVector",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Tags_SearchVector",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Tags");
        }
    }
}
