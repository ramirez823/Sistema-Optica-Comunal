using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SatisfaccionUsuario",
                table: "Tickets",
                newName: "ReabiertoPorId");

            migrationBuilder.RenameColumn(
                name: "RequiereSeguimiento",
                table: "Tickets",
                newName: "Reabierto");

            migrationBuilder.RenameColumn(
                name: "ComentariosAdicionales",
                table: "Tickets",
                newName: "ObservacionesInternas");

            migrationBuilder.AlterColumn<string>(
                name: "TiempoDedicado",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SolucionAplicada",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalificacionCliente",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComentarioCliente",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCalificacion",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaReapertura",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResolucion",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoReapertura",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalificacionCliente",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ComentarioCliente",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaCalificacion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaReapertura",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaResolucion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MotivoReapertura",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "ReabiertoPorId",
                table: "Tickets",
                newName: "SatisfaccionUsuario");

            migrationBuilder.RenameColumn(
                name: "Reabierto",
                table: "Tickets",
                newName: "RequiereSeguimiento");

            migrationBuilder.RenameColumn(
                name: "ObservacionesInternas",
                table: "Tickets",
                newName: "ComentariosAdicionales");

            migrationBuilder.AlterColumn<string>(
                name: "TiempoDedicado",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SolucionAplicada",
                table: "Tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
