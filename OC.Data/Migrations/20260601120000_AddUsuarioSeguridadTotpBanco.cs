using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    public partial class AddUsuarioSeguridadTotpBanco : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Banco",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarContrasena",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiracionToken",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenRecuperacion",
                table: "Usuarios",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TotpConfiguradoEnUtc",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TotpHabilitado",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecretProtegido",
                table: "Usuarios",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCuentaIBAN",
                table: "Usuarios",
                type: "nvarchar(22)",
                maxLength: 22,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Banco", table: "Usuarios");
            migrationBuilder.DropColumn(name: "DebeCambiarContrasena", table: "Usuarios");
            migrationBuilder.DropColumn(name: "FechaExpiracionToken", table: "Usuarios");
            migrationBuilder.DropColumn(name: "TokenRecuperacion", table: "Usuarios");
            migrationBuilder.DropColumn(name: "TotpConfiguradoEnUtc", table: "Usuarios");
            migrationBuilder.DropColumn(name: "TotpHabilitado", table: "Usuarios");
            migrationBuilder.DropColumn(name: "TotpSecretProtegido", table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCuentaIBAN",
                table: "Usuarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(22)",
                oldMaxLength: 22,
                oldNullable: true);
        }
    }
}
