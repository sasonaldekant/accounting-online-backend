using Microsoft.EntityFrameworkCore.Migrations;

namespace ERPAccounting.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds missing cost name and amount columns to tblDokumentTroskovi.
    /// </summary>
    public partial class AddDocumentCostAmountColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NazivTroska",
                table: "tblDokumentTroskovi",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IznosBezPDV",
                table: "tblDokumentTroskovi",
                type: "money",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IznosPDV",
                table: "tblDokumentTroskovi",
                type: "money",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NazivTroska",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "IznosBezPDV",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "IznosPDV",
                table: "tblDokumentTroskovi");
        }
    }
}
