using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJET.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRetourModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetourLignes");

            migrationBuilder.DropTable(
                name: "Retours");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Retours",
                columns: table => new
                {
                    RetourID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenteID = table.Column<int>(type: "int", nullable: false),
                    DateRetour = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Raison = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retours", x => x.RetourID);
                    table.ForeignKey(
                        name: "FK_Retours_Ventes_VenteID",
                        column: x => x.VenteID,
                        principalTable: "Ventes",
                        principalColumn: "VenteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetourLignes",
                columns: table => new
                {
                    RetourLigneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetourID = table.Column<int>(type: "int", nullable: false),
                    VenteLigneID = table.Column<int>(type: "int", nullable: false),
                    QuantiteRetournee = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetourLignes", x => x.RetourLigneID);
                    table.ForeignKey(
                        name: "FK_RetourLignes_Retours_RetourID",
                        column: x => x.RetourID,
                        principalTable: "Retours",
                        principalColumn: "RetourID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetourLignes_VenteLignes_VenteLigneID",
                        column: x => x.VenteLigneID,
                        principalTable: "VenteLignes",
                        principalColumn: "VenteLigneID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetourLignes_RetourID",
                table: "RetourLignes",
                column: "RetourID");

            migrationBuilder.CreateIndex(
                name: "IX_RetourLignes_VenteLigneID",
                table: "RetourLignes",
                column: "VenteLigneID");

            migrationBuilder.CreateIndex(
                name: "IX_Retours_VenteID",
                table: "Retours",
                column: "VenteID");
        }
    }
}
