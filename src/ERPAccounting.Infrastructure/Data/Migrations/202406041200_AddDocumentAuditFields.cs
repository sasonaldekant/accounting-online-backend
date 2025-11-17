using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ERPAccounting.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Dodaje Created*/Updated* audit kolone i IsDeleted flag na tblDokument i tblStavkaDokumenta
    /// kako bi soft-delete filteri radili.
    /// </summary>
    public partial class AddDocumentAuditFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "tblDokument",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "tblDokument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tblDokument",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "tblDokument",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "tblDokument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "tblStavkaDokumenta",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "tblStavkaDokumenta",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tblStavkaDokumenta",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "tblStavkaDokumenta",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "tblStavkaDokumenta",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tblDokument");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "tblDokument");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tblDokument");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "tblDokument");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "tblDokument");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tblStavkaDokumenta");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "tblStavkaDokumenta");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tblStavkaDokumenta");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "tblStavkaDokumenta");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "tblStavkaDokumenta");
        }
    }
}
