using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJET.Migrations
{
    /// <inheritdoc />
    public partial class AddProduitDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Produits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Produits");
        }
    }
}
