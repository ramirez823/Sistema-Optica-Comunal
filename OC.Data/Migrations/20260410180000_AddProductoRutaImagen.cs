using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    [Migration("20260410180000_AddProductoRutaImagen")]
    public partial class AddProductoRutaImagen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Productos') AND name = 'RutaImagen'
)
BEGIN
    ALTER TABLE Productos ADD RutaImagen nvarchar(512) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Productos') AND name = 'RutaImagen'
)
BEGIN
    ALTER TABLE Productos DROP COLUMN RutaImagen;
END
");
        }
    }
}
