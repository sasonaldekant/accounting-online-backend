using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ERPAccounting.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Dodaje audit kolone i IsDeleted flagu za tblDokumentTroskovi i tblDokumentTroskoviStavka
    /// kako bi soft delete filter radio dosledno.
    /// </summary>
    public partial class AddDocumentCostAuditFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "tblDokumentTroskovi",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "tblDokumentTroskovi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tblDokumentTroskovi",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "tblDokumentTroskovi",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "tblDokumentTroskovi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "tblDokumentTroskoviStavka",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "tblDokumentTroskoviStavka",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tblDokumentTroskoviStavka",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "tblDokumentTroskoviStavka",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "tblDokumentTroskoviStavka",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "tblDokumentTroskovi");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tblDokumentTroskoviStavka");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "tblDokumentTroskoviStavka");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tblDokumentTroskoviStavka");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "tblDokumentTroskoviStavka");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "tblDokumentTroskoviStavka");
        }
    }
}
