using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpedienteTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentoExpediente_Expediente_ExpedienteId",
                table: "DocumentoExpediente");

            migrationBuilder.DropForeignKey(
                name: "FK_Expediente_Citas_CitaId",
                table: "Expediente");

            migrationBuilder.DropForeignKey(
                name: "FK_ValorClinico_Expediente_ExpedienteId",
                table: "ValorClinico");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ValorClinico",
                table: "ValorClinico");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expediente",
                table: "Expediente");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentoExpediente",
                table: "DocumentoExpediente");

            migrationBuilder.RenameTable(
                name: "ValorClinico",
                newName: "ValoresClinicos");

            migrationBuilder.RenameTable(
                name: "Expediente",
                newName: "Expedientes");

            migrationBuilder.RenameTable(
                name: "DocumentoExpediente",
                newName: "DocumentosExpediente");

            migrationBuilder.RenameIndex(
                name: "IX_ValorClinico_ExpedienteId",
                table: "ValoresClinicos",
                newName: "IX_ValoresClinicos_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_Expediente_CitaId",
                table: "Expedientes",
                newName: "IX_Expedientes_CitaId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentoExpediente_ExpedienteId",
                table: "DocumentosExpediente",
                newName: "IX_DocumentosExpediente_ExpedienteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValoresClinicos",
                table: "ValoresClinicos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expedientes",
                table: "Expedientes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentosExpediente",
                table: "DocumentosExpediente",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosExpediente_Expedientes_ExpedienteId",
                table: "DocumentosExpediente",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_Citas_CitaId",
                table: "Expedientes",
                column: "CitaId",
                principalTable: "Citas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ValoresClinicos_Expedientes_ExpedienteId",
                table: "ValoresClinicos",
                column: "ExpedienteId",
                principalTable: "Expedientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosExpediente_Expedientes_ExpedienteId",
                table: "DocumentosExpediente");

            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_Citas_CitaId",
                table: "Expedientes");

            migrationBuilder.DropForeignKey(
                name: "FK_ValoresClinicos_Expedientes_ExpedienteId",
                table: "ValoresClinicos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ValoresClinicos",
                table: "ValoresClinicos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expedientes",
                table: "Expedientes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentosExpediente",
                table: "DocumentosExpediente");

            migrationBuilder.RenameTable(
                name: "ValoresClinicos",
                newName: "ValorClinico");

            migrationBuilder.RenameTable(
                name: "Expedientes",
                newName: "Expediente");

            migrationBuilder.RenameTable(
                name: "DocumentosExpediente",
                newName: "DocumentoExpediente");

            migrationBuilder.RenameIndex(
                name: "IX_ValoresClinicos_ExpedienteId",
                table: "ValorClinico",
                newName: "IX_ValorClinico_ExpedienteId");

            migrationBuilder.RenameIndex(
                name: "IX_Expedientes_CitaId",
                table: "Expediente",
                newName: "IX_Expediente_CitaId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentosExpediente_ExpedienteId",
                table: "DocumentoExpediente",
                newName: "IX_DocumentoExpediente_ExpedienteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValorClinico",
                table: "ValorClinico",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expediente",
                table: "Expediente",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentoExpediente",
                table: "DocumentoExpediente",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentoExpediente_Expediente_ExpedienteId",
                table: "DocumentoExpediente",
                column: "ExpedienteId",
                principalTable: "Expediente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expediente_Citas_CitaId",
                table: "Expediente",
                column: "CitaId",
                principalTable: "Citas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ValorClinico_Expediente_ExpedienteId",
                table: "ValorClinico",
                column: "ExpedienteId",
                principalTable: "Expediente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
