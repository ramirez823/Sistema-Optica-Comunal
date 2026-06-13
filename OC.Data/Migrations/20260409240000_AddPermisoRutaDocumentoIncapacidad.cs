using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <summary>Documento adjunto opcional para permisos por incapacidad (RR.HH.).</summary>
    [Migration("20260409240000_AddPermisoRutaDocumentoIncapacidad")]
    public partial class AddPermisoRutaDocumentoIncapacidad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Permisos') AND name = 'RutaDocumentoIncapacidad'
)
BEGIN
    ALTER TABLE Permisos ADD RutaDocumentoIncapacidad nvarchar(512) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Permisos') AND name = 'RutaDocumentoIncapacidad'
)
BEGIN
    ALTER TABLE Permisos DROP COLUMN RutaDocumentoIncapacidad;
END
");
        }
    }
}
