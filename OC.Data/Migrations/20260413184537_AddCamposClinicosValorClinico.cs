using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposClinicosValorClinico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvOdCerca",
                table: "ValoresClinicos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvOdLejos",
                table: "ValoresClinicos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvOiCerca",
                table: "ValoresClinicos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvOiLejos",
                table: "ValoresClinicos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CampoVisual",
                table: "ValoresClinicos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FondoOjo",
                table: "ValoresClinicos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotilidadOcular",
                table: "ValoresClinicos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "ValoresClinicos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PercepcionColores",
                table: "ValoresClinicos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PioOd",
                table: "ValoresClinicos",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PioOi",
                table: "ValoresClinicos",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvOdCerca",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "AvOdLejos",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "AvOiCerca",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "AvOiLejos",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "CampoVisual",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "FondoOjo",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "MotilidadOcular",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "PercepcionColores",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "PioOd",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "PioOi",
                table: "ValoresClinicos");
        }
    }
}
