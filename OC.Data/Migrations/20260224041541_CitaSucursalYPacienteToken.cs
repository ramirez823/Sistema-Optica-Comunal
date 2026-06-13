using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class CitaSucursalYPacienteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Cedula",
                table: "Pacientes",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiracionToken",
                table: "Pacientes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenRecuperacion",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Citas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Citas_SucursalId",
                table: "Citas",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Sucursales_SucursalId",
                table: "Citas",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Sucursales_SucursalId",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_SucursalId",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "FechaExpiracionToken",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "TokenRecuperacion",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Citas");

            migrationBuilder.AlterColumn<string>(
                name: "Cedula",
                table: "Pacientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9);
        }
    }
}
