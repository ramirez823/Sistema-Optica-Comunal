using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    public partial class LandingPageCamposAdicionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HorarioAtencion",
                table: "Sucursales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitud",
                table: "Sucursales",
                type: "decimal(10,6)",
                precision: 10,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitud",
                table: "Sucursales",
                type: "decimal(10,6)",
                precision: 10,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoAdicional",
                table: "Sucursales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionCorta",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Destacado",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioPublico",
                table: "Productos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HorarioAtencion",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "TelefonoAdicional",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DescripcionCorta",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Destacado",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PrecioPublico",
                table: "Productos");
        }
    }
}
