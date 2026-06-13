using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <summary>
    /// Sincroniza el snapshot de EF con el esquema real:
    ///  - Agrega ADD_Od / ADD_Oi (decimal(5,2) NULL) a ValoresClinicos
    ///  - Convierte EjeOD / EjeOI de decimal(4,2) a int (clínicamente 0..180)
    /// El bloque EnsureValorClinicoAddColumns en DbInitializer sigue siendo la red
    /// de seguridad para bases ya existentes con historial de migraciones inconsistente.
    /// </summary>
    public partial class AddValorClinicoADDAndEjeInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EjeOD",
                table: "ValoresClinicos",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EjeOI",
                table: "ValoresClinicos",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ADD_Od",
                table: "ValoresClinicos",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ADD_Oi",
                table: "ValoresClinicos",
                type: "decimal(5,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ADD_Od",
                table: "ValoresClinicos");

            migrationBuilder.DropColumn(
                name: "ADD_Oi",
                table: "ValoresClinicos");

            migrationBuilder.AlterColumn<decimal>(
                name: "EjeOD",
                table: "ValoresClinicos",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "EjeOI",
                table: "ValoresClinicos",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
