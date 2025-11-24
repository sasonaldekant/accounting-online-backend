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
            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'NazivTroska') IS NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          ADD [NazivTroska] varchar(255) NULL;
                  END");

            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'IznosBezPDV') IS NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          ADD [IznosBezPDV] money NOT NULL
                              CONSTRAINT [DF_tblDokumentTroskovi_IznosBezPDV] DEFAULT 0;
                  END");

            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'IznosPDV') IS NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          ADD [IznosPDV] money NOT NULL
                              CONSTRAINT [DF_tblDokumentTroskovi_IznosPDV] DEFAULT 0;
                  END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'NazivTroska') IS NOT NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          DROP COLUMN [NazivTroska];
                  END");

            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'IznosBezPDV') IS NOT NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          DROP COLUMN [IznosBezPDV];
                  END");

            migrationBuilder.Sql(
                @"IF COL_LENGTH('tblDokumentTroskovi', 'IznosPDV') IS NOT NULL
                  BEGIN
                      ALTER TABLE [tblDokumentTroskovi]
                          DROP COLUMN [IznosPDV];
                  END");
        }
    }
}
