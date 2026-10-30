using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechShop.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVenteLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LivreurID",
                table: "Ventes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Ventes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ventes_LivreurID",
                table: "Ventes",
                column: "LivreurID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventes_Livreurs_LivreurID",
                table: "Ventes",
                column: "LivreurID",
                principalTable: "Livreurs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventes_Livreurs_LivreurID",
                table: "Ventes");

            migrationBuilder.DropIndex(
                name: "IX_Ventes_LivreurID",
                table: "Ventes");

            migrationBuilder.DropColumn(
                name: "LivreurID",
                table: "Ventes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Ventes");
        }
    }
}
