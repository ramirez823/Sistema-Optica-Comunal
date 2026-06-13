using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    public partial class AddNumeroToProveedores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroTelefonico",
                table: "Proveedores",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Proveedores",
                type: "nvarchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "ALTER TABLE dbo.Proveedores ADD CONSTRAINT CK_Proveedores_NumeroTelefonico CHECK (NumeroTelefonico BETWEEN 0 AND 99999999)"
            );

            migrationBuilder.Sql(
                "ALTER TABLE dbo.Proveedores ADD CONSTRAINT CK_Proveedores_Correo CHECK (Correo LIKE '%@%.%')"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE dbo.Proveedores DROP CONSTRAINT CK_Proveedores_NumeroTelefonico"
            );

            migrationBuilder.Sql(
                "ALTER TABLE dbo.Proveedores DROP CONSTRAINT CK_Proveedores_Correo"
            );
            migrationBuilder.DropColumn(
                name: "NumeroTelefonico",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Proveedores");
        }
    }
}
