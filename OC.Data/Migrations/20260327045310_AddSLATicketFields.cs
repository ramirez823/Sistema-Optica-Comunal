using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSLATicketFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComentariosAdicionales",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPrimeraRespuesta",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResolucionEsperada",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRespuestaEsperada",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimaAlertaSLA",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereSeguimiento",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SLA_CumplidoResolucion",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SLA_CumplidoRespuesta",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SLA_Observacion",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SatisfaccionUsuario",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolucionAplicada",
                table: "Tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiempoDedicado",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComentariosAdicionales",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaPrimeraRespuesta",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaResolucionEsperada",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaRespuestaEsperada",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaUltimaAlertaSLA",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RequiereSeguimiento",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SLA_CumplidoResolucion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SLA_CumplidoRespuesta",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SLA_Observacion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SatisfaccionUsuario",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SolucionAplicada",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TiempoDedicado",
                table: "Tickets");
        }
    }
}
