using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities;

namespace OC.Data.Context
{
    public static class DbInitializer
    {
        /// <summary>Crea la tabla OrdenesTrabajo si no existe (respaldo cuando la migración no se aplica correctamente).</summary>
        public static void EnsureOrdenesTrabajoTable(AppDbContext context)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdenesTrabajo')
BEGIN
    CREATE TABLE [OrdenesTrabajo] (
        [Id] int NOT NULL IDENTITY(1,1),
        [PacienteId] int NOT NULL,
        [SucursalId] int NOT NULL,
        [VentaId] int NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Referencia] nvarchar(200) NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaLista] datetime2 NULL,
        CONSTRAINT [PK_OrdenesTrabajo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrdenesTrabajo_Pacientes_PacienteId] FOREIGN KEY ([PacienteId]) REFERENCES [Pacientes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrdenesTrabajo_Sucursales_SucursalId] FOREIGN KEY ([SucursalId]) REFERENCES [Sucursales]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrdenesTrabajo_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_OrdenesTrabajo_PacienteId] ON [OrdenesTrabajo]([PacienteId]);
    CREATE INDEX [IX_OrdenesTrabajo_SucursalId] ON [OrdenesTrabajo]([SucursalId]);
    CREATE INDEX [IX_OrdenesTrabajo_VentaId] ON [OrdenesTrabajo]([VentaId]);
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Crea/ajusta la tabla EnviosNotificacion para que OT-HU-023 pueda registrar notificaciones.</summary>
        public static void EnsureEnviosNotificacionTable(AppDbContext context)
        {
            // Nota: evitamos depender de migraciones que alteren/crean esta tabla.
            // Ajustamos mínimamente para que Inserte con CitaId/OrdenTrabajoId NULL permitidos.
            var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EnviosNotificacion')
BEGIN
    CREATE TABLE [EnviosNotificacion] (
        [Id] int NOT NULL IDENTITY(1,1),
        [CitaId] int NULL,
        [OrdenTrabajoId] int NULL,
        [TipoNotificacion] nvarchar(50) NOT NULL,
        [FechaHoraEnvio] datetime2 NOT NULL,
        [Canal] nvarchar(20) NOT NULL DEFAULT ('Email'),
        [Destinatario] nvarchar(256) NULL,
        [MensajeResumen] nvarchar(500) NULL,
        [Exito] bit NOT NULL DEFAULT (1),
        CONSTRAINT [PK_EnviosNotificacion] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_EnviosNotificacion_CitaId] ON [EnviosNotificacion] ([CitaId]);
    CREATE INDEX [IX_EnviosNotificacion_OrdenTrabajoId] ON [EnviosNotificacion] ([OrdenTrabajoId]);
END
ELSE
BEGIN
    -- Si falta OrdenTrabajoId (p. ej. por migraciones cortadas), lo agregamos.
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EnviosNotificacion') AND name = 'OrdenTrabajoId')
    BEGIN
        ALTER TABLE [EnviosNotificacion] ADD [OrdenTrabajoId] int NULL;
        CREATE INDEX [IX_EnviosNotificacion_OrdenTrabajoId] ON [EnviosNotificacion] ([OrdenTrabajoId]);
    END

    -- OT-HU-023 inserta registros con CitaId = NULL; CitaId debe ser nullable.
    IF EXISTS (
        SELECT * FROM sys.columns
        WHERE object_id = OBJECT_ID('EnviosNotificacion') AND name = 'CitaId' AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [EnviosNotificacion] ALTER COLUMN [CitaId] int NULL;
        -- No es necesario recrear índices; SQL Server los conserva.
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Crea columnas de notificación en Citas si faltan en la BD.</summary>
        public static void EnsureCitasNotificationColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Citas','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Citas') AND name = 'NotificacionesActivas'
    )
    BEGIN
        ALTER TABLE Citas ADD NotificacionesActivas bit NOT NULL CONSTRAINT DF_Citas_NotificacionesActivas DEFAULT (1);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Citas') AND name = 'CanalNotificacion'
    )
    BEGIN
        ALTER TABLE Citas ADD CanalNotificacion nvarchar(20) NOT NULL CONSTRAINT DF_Citas_CanalNotificacion DEFAULT ('Email');
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>WEB-HU-028: asegura columnas para bloqueo por intentos fallidos en Pacientes.</summary>
        public static void EnsurePacienteLockoutColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Pacientes','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Pacientes') AND name = 'IntentosFallidosLogin'
    )
    BEGIN
        ALTER TABLE Pacientes ADD IntentosFallidosLogin int NOT NULL CONSTRAINT DF_Pacientes_IntentosFallidosLogin DEFAULT (0);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Pacientes') AND name = 'BloqueadoHastaUtc'
    )
    BEGIN
        ALTER TABLE Pacientes ADD BloqueadoHastaUtc datetime2 NULL;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Pacientes') AND name = 'BloqueadoPermanentemente'
    )
    BEGIN
        ALTER TABLE Pacientes ADD BloqueadoPermanentemente bit NOT NULL CONSTRAINT DF_Pacientes_BloqueadoPermanentemente DEFAULT (0);
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Columnas de seguridad (TOTP, contraseña temporal, banco) en Usuarios.</summary>
        public static void EnsureUsuarioSeguridadColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Usuarios','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Banco')
        ALTER TABLE Usuarios ADD Banco nvarchar(100) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'DebeCambiarContrasena')
        ALTER TABLE Usuarios ADD DebeCambiarContrasena bit NOT NULL CONSTRAINT DF_Usuarios_DebeCambiarContrasena DEFAULT (0);

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'TotpHabilitado')
        ALTER TABLE Usuarios ADD TotpHabilitado bit NOT NULL CONSTRAINT DF_Usuarios_TotpHabilitado DEFAULT (0);

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'TotpSecretProtegido')
        ALTER TABLE Usuarios ADD TotpSecretProtegido nvarchar(1024) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'TotpConfiguradoEnUtc')
        ALTER TABLE Usuarios ADD TotpConfiguradoEnUtc datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'TokenRecuperacion')
        ALTER TABLE Usuarios ADD TokenRecuperacion nvarchar(64) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'FechaExpiracionToken')
        ALTER TABLE Usuarios ADD FechaExpiracionToken datetime2 NULL;
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Columna para documento de incapacidad en permisos (RR.HH.).</summary>
        public static void EnsureProductoRutaImagenColumn(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Productos','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Productos') AND name = 'RutaImagen'
    )
    BEGIN
        ALTER TABLE Productos ADD RutaImagen nvarchar(512) NULL;
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        public static void EnsurePermisoRutaDocumentoIncapacidadColumn(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Permisos','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Permisos') AND name = 'RutaDocumentoIncapacidad'
    )
    BEGIN
        ALTER TABLE Permisos ADD RutaDocumentoIncapacidad nvarchar(512) NULL;
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        public static void EnsureValorClinicoAddColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('ValoresClinicos','U') IS NOT NULL
BEGIN
    -- ADD columns (may already exist from prior run)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ValoresClinicos') AND name = 'ADD_Od')
        ALTER TABLE ValoresClinicos ADD ADD_Od decimal(5,2) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ValoresClinicos') AND name = 'ADD_Oi')
        ALTER TABLE ValoresClinicos ADD ADD_Oi decimal(5,2) NULL;

    -- EjeOD / EjeOI: change from decimal(4,2) to int (clinically integers 0-180)
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ValoresClinicos') AND name = 'EjeOD' AND type_name(user_type_id) = 'decimal')
        ALTER TABLE ValoresClinicos ALTER COLUMN EjeOD int NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ValoresClinicos') AND name = 'EjeOI' AND type_name(user_type_id) = 'decimal')
        ALTER TABLE ValoresClinicos ALTER COLUMN EjeOI int NULL;
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        public static void EnsureOrdenTrabajoNewColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('OrdenesTrabajo','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesTrabajo') AND name = 'PD')
        ALTER TABLE OrdenesTrabajo ADD PD decimal(4,1) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesTrabajo') AND name = 'TipoLente')
        ALTER TABLE OrdenesTrabajo ADD TipoLente nvarchar(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesTrabajo') AND name = 'MaterialLente')
        ALTER TABLE OrdenesTrabajo ADD MaterialLente nvarchar(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesTrabajo') AND name = 'Tratamientos')
        ALTER TABLE OrdenesTrabajo ADD Tratamientos nvarchar(500) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesTrabajo') AND name = 'LaboratorioExterno')
        ALTER TABLE OrdenesTrabajo ADD LaboratorioExterno nvarchar(200) NULL;
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>
        /// Red de seguridad para BDs con historial de migraciones inconsistente:
        /// - Crea la tabla Proveedores si falta.
        /// - Suelta los CHECK constraints naive (NumeroTelefonico BETWEEN 0..99999999 y Correo LIKE '%@%.%').
        /// - Convierte NumeroTelefonico de int a nvarchar(9) si todavía es int.
        /// Las validaciones reales viven en la entidad Proveedor.
        /// </summary>
        public static void EnsureProveedorSchema(AppDbContext context)
        {
            var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Proveedores')
BEGIN
    CREATE TABLE [Proveedores] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Nombre] nvarchar(150) NOT NULL,
        [NumeroTelefonico] nvarchar(9) NOT NULL,
        [Correo] nvarchar(255) NOT NULL,
        [Activo] bit NOT NULL DEFAULT (1),
        [ContactoAdicionalNombre] nvarchar(100) NULL,
        [ContactoAdicionalTelefono] nvarchar(20) NULL,
        CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    -- Sueltas los CHECK constraints naive si existen
    IF OBJECT_ID('dbo.CK_Proveedores_NumeroTelefonico','C') IS NOT NULL
        ALTER TABLE [dbo].[Proveedores] DROP CONSTRAINT [CK_Proveedores_NumeroTelefonico];
    IF OBJECT_ID('dbo.CK_Proveedores_Correo','C') IS NOT NULL
        ALTER TABLE [dbo].[Proveedores] DROP CONSTRAINT [CK_Proveedores_Correo];

    -- Convierte NumeroTelefonico de int a nvarchar(9) si está como int
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Proveedores')
          AND name = 'NumeroTelefonico'
          AND system_type_id = 56  -- int
    )
    BEGIN
        ALTER TABLE [dbo].[Proveedores] ALTER COLUMN [NumeroTelefonico] nvarchar(9) NOT NULL;
    END

    -- Columnas de contacto adicional (idempotente)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'ContactoAdicionalNombre')
        ALTER TABLE [dbo].[Proveedores] ADD [ContactoAdicionalNombre] nvarchar(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'ContactoAdicionalTelefono')
        ALTER TABLE [dbo].[Proveedores] ADD [ContactoAdicionalTelefono] nvarchar(20) NULL;
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        public static void Initialize(AppDbContext context)
        {
            // 1. Solo crear sucursal si no hay ninguna
            if (!context.Sucursales.Any())
            {
                context.Sucursales.Add(new Sucursal
                {
                    Nombre = "Sucursal Matriz",
                    Direccion = "...",
                    Telefono = "...",
                    Activo = true // Importante: UI de Órdenes filtra por Activo
                });
                context.SaveChanges();
            }

            // 2. Solo crear roles si la tabla está vacía
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Rol { Nombre = "Admin", Descripcion = "Acceso Total" },
                    new Rol { Nombre = "Optometrista", Descripcion = "Personal Médico" },
                    new Rol { Nombre = "Recepcion", Descripcion = "Atención al Cliente" },
                    new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" },
                    new Rol { Nombre = "Tecnico", Descripcion = "Técnico de soporte" }
                );
                context.SaveChanges();
            }
            else
            {
                if (!context.Roles.Any(r => r.Nombre == "Paciente"))
                {
                    context.Roles.Add(new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" });
                    context.SaveChanges();
                }
                if (!context.Roles.Any(r => r.Nombre == "Tecnico"))
                {
                    context.Roles.Add(new Rol { Nombre = "Tecnico", Descripcion = "Técnico de soporte" });
                    context.SaveChanges();
                }
                if (!context.Roles.Any(r => r.Nombre == "TecnicoOcular"))
                {
                    context.Roles.Add(new Rol { Nombre = "TecnicoOcular", Descripcion = "Técnico de lentes" });
                    context.SaveChanges();
                }
            }

            // 3. Solo crear el admin si no existe el correo
            if (!context.Usuarios.Any(u => u.Correo == "admin@optica.com"))
            {
                var rolAdmin = context.Roles.First(r => r.Nombre == "Admin");
                var sucursal = context.Sucursales.First();

                context.Usuarios.Add(new Usuario
                {
                    Nombre = "Administrador",
                    Correo = "admin@optica.com",
                    Cedula = "123456789",
                    Contrasena = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Activo = true,
                    RolId = rolAdmin.Id,
                    SucursalId = sucursal.Id,
                    SalarioBase = 350000,
                    FechaContratacion = DateTime.Now.AddYears(-3)
                });
                context.SaveChanges();
            }

            // 4. Seed de tecnologias de lentes si no existen
            if (!context.TecnologiaLentes.Any())
            {
                var tecnologias = new[]
                {
                    new TecnologiaLente { Nombre = "Monofocal" },
                    new TecnologiaLente { Nombre = "Bifocal" },
                    new TecnologiaLente { Nombre = "Progresivo" },
                    new TecnologiaLente { Nombre = "Fotocromático" },
                    new TecnologiaLente { Nombre = "Anti-reflejo" },
                    new TecnologiaLente { Nombre = "Blue Cut" },
                    new TecnologiaLente { Nombre = "Fotocromático + Anti-reflejo" },
                    new TecnologiaLente { Nombre = "Lentes de contacto" }
                };
                context.TecnologiaLentes.AddRange(tecnologias);
                context.SaveChanges();
            }
        }
    }
}