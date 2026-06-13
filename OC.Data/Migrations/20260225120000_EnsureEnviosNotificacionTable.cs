using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <summary>
    /// Crea la tabla EnviosNotificacion solo si no existe (p. ej. BD nueva).
    /// Algunas BDs la tenían por EnsureCreated o por una migración previa eliminada.
    /// La migración AddOrdenTrabajoYNotifLentesListos altera esta tabla; sin ella fallaba.
    /// </summary>
    [Migration("20260225120000_EnsureEnviosNotificacionTable")]
    public partial class EnsureEnviosNotificacionTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EnviosNotificacion')
BEGIN
    CREATE TABLE [EnviosNotificacion] (
        [Id] int NOT NULL IDENTITY,
        [CitaId] int NOT NULL,
        [TipoNotificacion] nvarchar(50) NOT NULL,
        [FechaHoraEnvio] datetime2 NOT NULL,
        [Canal] nvarchar(20) NOT NULL DEFAULT 'Email',
        [Destinatario] nvarchar(256) NULL,
        [MensajeResumen] nvarchar(500) NULL,
        [Exito] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_EnviosNotificacion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EnviosNotificacion_Citas_CitaId] FOREIGN KEY ([CitaId]) REFERENCES [Citas] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_EnviosNotificacion_CitaId] ON [EnviosNotificacion] ([CitaId]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se elimina la tabla para no afectar BDs que ya la tenían antes de esta migración.
        }
    }
}
