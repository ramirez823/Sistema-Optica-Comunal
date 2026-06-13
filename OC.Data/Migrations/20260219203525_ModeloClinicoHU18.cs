using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModeloClinicoHU18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expediente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitaId = table.Column<int>(type: "int", nullable: false),
                    MotivoConsulta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expediente_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoExpediente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoExpediente_Expediente_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValorClinico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    Diagnostico = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsferaOD = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    CilindroOD = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    EjeOD = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    EsferaOI = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    CilindroOI = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    EjeOI = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValorClinico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValorClinico_Expediente_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExpediente_ExpedienteId",
                table: "DocumentoExpediente",
                column: "ExpedienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Expediente_CitaId",
                table: "Expediente",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValorClinico_ExpedienteId",
                table: "ValorClinico",
                column: "ExpedienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentoExpediente");

            migrationBuilder.DropTable(
                name: "ValorClinico");

            migrationBuilder.DropTable(
                name: "Expediente");
        }
    }
}
