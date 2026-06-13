using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <summary>
    /// Cambia NumeroTelefonico de int a nvarchar(9) y elimina los CHECK
    /// constraints naive (NumeroTelefonico BETWEEN 0..99999999 y
    /// Correo LIKE '%@%.%'). Las validaciones ahora viven en la entidad
    /// con [PhoneCR]-style y [EmailAddress] (revisar OC.Core/Domain/Entities/Proveedor.cs).
    ///
    /// Los demás cambios "huérfanos" del modelo (ValorClinico.Eje*, ADD_*,
    /// OrdenesTrabajo.LaboratorioExterno/etc., Usuario.Banco/Totp*/etc.)
    /// NO se incluyen aquí porque ya los aplican los métodos Ensure* de
    /// DbInitializer al arranque. El snapshot se mantiene alineado con el
    /// modelo actual para que futuras migraciones solo capturen diffs reales.
    /// </summary>
    public partial class ProveedorPhoneToStringAndDropCheckConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Quitar los CHECK constraints viejos (de la migración
            //    20260319165008_AddNumeroToProveedores) antes de alterar la columna.
            migrationBuilder.Sql("ALTER TABLE [Proveedores] DROP CONSTRAINT [CK_Proveedores_NumeroTelefonico]");
            migrationBuilder.Sql("ALTER TABLE [Proveedores] DROP CONSTRAINT [CK_Proveedores_Correo]");

            // 2) Cambiar NumeroTelefonico de int a nvarchar(9). SQL Server convierte
            //    los valores existentes implícitamente (ej: 22334455 -> '22334455').
            migrationBuilder.AlterColumn<string>(
                name: "NumeroTelefonico",
                table: "Proveedores",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1) Volver a int (asumiendo que el contenido cabe en int).
            migrationBuilder.AlterColumn<int>(
                name: "NumeroTelefonico",
                table: "Proveedores",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9);

            // 2) Restaurar los CHECK constraints originales.
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[Proveedores] ADD CONSTRAINT [CK_Proveedores_NumeroTelefonico] CHECK (NumeroTelefonico BETWEEN 0 AND 99999999)");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[Proveedores] ADD CONSTRAINT [CK_Proveedores_Correo] CHECK (Correo LIKE '%@%.%')");
        }
    }
}
