using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJET.Migrations
{
    /// <inheritdoc />
    public partial class VenteModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ventes",
                columns: table => new
                {
                    VenteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateVente = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventes", x => x.VenteID);
                });

            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    FactureID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroFacture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateFacture = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenteID = table.Column<int>(type: "int", nullable: false),
                    TypeClient = table.Column<int>(type: "int", nullable: false),
                    NomClient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MatriculeFiscal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MontantHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTVA = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Timbre = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTTC = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.FactureID);
                    table.ForeignKey(
                        name: "FK_Factures_Ventes_VenteID",
                        column: x => x.VenteID,
                        principalTable: "Ventes",
                        principalColumn: "VenteID",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "VenteLignes",
                columns: table => new
                {
                    VenteLigneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenteID = table.Column<int>(type: "int", nullable: false),
                    ProduitID = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenteLignes", x => x.VenteLigneID);
                    table.ForeignKey(
                        name: "FK_VenteLignes_Ventes_VenteID",
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
                name: "IX_Factures_NumeroFacture",
                table: "Factures",
                column: "NumeroFacture",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_VenteID",
                table: "Factures",
                column: "VenteID",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_VenteLignes_VenteID",
                table: "VenteLignes",
                column: "VenteID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Factures");

            migrationBuilder.DropTable(
                name: "RetourLignes");

            migrationBuilder.DropTable(
                name: "Retours");

            migrationBuilder.DropTable(
                name: "VenteLignes");

            migrationBuilder.DropTable(
                name: "Ventes");
        }
    }
}
