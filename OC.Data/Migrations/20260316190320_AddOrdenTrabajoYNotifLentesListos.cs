using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>Orden de trabajo y notificación lentes listos (OT-HU-023). Debe ejecutarse después de AddModuloVentas (FK a Ventas).</summary>
    [Migration("20260316190320_AddOrdenTrabajoYNotifLentesListos")]
    public partial class AddOrdenTrabajoYNotifLentesListos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenesTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaLista = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_PacienteId",
                table: "OrdenesTrabajo",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_SucursalId",
                table: "OrdenesTrabajo",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_VentaId",
                table: "OrdenesTrabajo",
                column: "VentaId");

            migrationBuilder.AlterColumn<int>(
                name: "CitaId",
                table: "EnviosNotificacion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "OrdenTrabajoId",
                table: "EnviosNotificacion",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnviosNotificacion_OrdenTrabajoId",
                table: "EnviosNotificacion",
                column: "OrdenTrabajoId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnviosNotificacion_OrdenesTrabajo_OrdenTrabajoId",
                table: "EnviosNotificacion",
                column: "OrdenTrabajoId",
                principalTable: "OrdenesTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnviosNotificacion_OrdenesTrabajo_OrdenTrabajoId",
                table: "EnviosNotificacion");

            migrationBuilder.DropIndex(
                name: "IX_EnviosNotificacion_OrdenTrabajoId",
                table: "EnviosNotificacion");

            migrationBuilder.DropColumn(
                name: "OrdenTrabajoId",
                table: "EnviosNotificacion");

            migrationBuilder.AlterColumn<int>(
                name: "CitaId",
                table: "EnviosNotificacion",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "OrdenesTrabajo");
        }
    }
}
