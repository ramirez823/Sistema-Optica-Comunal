using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities;
using System.Reflection;
using Microsoft.EntityFrameworkCore;


namespace OC.Data.Context
{
    public class AppDbContext : DbContext


    {
        // OJO AQUÍ: Debe decir <AppDbContext> dentro de los símbolos menor/mayor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<SolicitudCita> SolicitudesCitas { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Producto> Productos { get; set; }


        public DbSet<Expediente> Expedientes { get; set; }
        public DbSet<ValorClinico> ValoresClinicos { get; set; }
        public DbSet<DocumentoExpediente> DocumentosExpediente { get; set; }
        public DbSet<EnvioNotificacion> EnviosNotificacion { get; set; }
        public DbSet<OrdenTrabajo> OrdenesTrabajo { get; set; }

        public DbSet<DetallePedido> DetallePedidos { get; set; }

        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<ComentarioTicket> ComentarioTickets { get; set; }


        //facturacion
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }

        //RH
        public DbSet<Planilla> Planillas { get; set; }

        //Asistencia
        public DbSet<Asistencia> Asistencias { get; set; }

        //Permisos
        public DbSet<Permiso> Permisos { get; set; }

        //Lentes y Aros
        public DbSet<TecnologiaLente> TecnologiaLentes { get; set; }
        public DbSet<Aro> Aros { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<ValorClinico>(entity =>
            {
                // Campos de refracción
                entity.Property(e => e.EsferaOD).HasPrecision(4, 2);
                entity.Property(e => e.CilindroOD).HasPrecision(4, 2);
                entity.Property(e => e.EjeOD).HasColumnType("int");
                entity.Property(e => e.EsferaOI).HasPrecision(4, 2);
                entity.Property(e => e.CilindroOI).HasPrecision(4, 2);
                entity.Property(e => e.EjeOI).HasColumnType("int");
                // Addition (ADD)
                entity.Property(e => e.ADD_Od).HasPrecision(5, 2);
                entity.Property(e => e.ADD_Oi).HasPrecision(5, 2);
                // Presión intraocular
                entity.Property(e => e.PioOd).HasPrecision(4, 1);
                entity.Property(e => e.PioOi).HasPrecision(4, 1);
            });

            //LandingPage campos adicionales digales praaa
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.Property(p => p.PrecioPublico).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Sucursal>(entity =>
            {
                entity.Property(s => s.Latitud).HasPrecision(10, 6);
                entity.Property(s => s.Longitud).HasPrecision(10, 6);
            });

            modelBuilder.Entity<Expediente>()
                .HasOne(e => e.Cita)
                .WithOne(c => c.Expediente)
                .HasForeignKey<Expediente>(e => e.CitaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Sucursal)
                .WithMany(s => s.Citas)
                .HasForeignKey(c => c.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paciente>(e =>
            {
                e.Property(p => p.Cedula).HasMaxLength(9);
                e.Property(p => p.TotpSecretProtegido).HasMaxLength(1024);
            });

            modelBuilder.Entity<Producto>(e =>
            {
                e.HasIndex(p => p.SKU).IsUnique();
                e.Property(p => p.CostoUnitario).HasPrecision(18, 2);
                e.Property(p => p.RutaImagen).HasMaxLength(512);
            });

            modelBuilder.Entity<DetallePedido>(entity =>
            {
                entity.Property(e => e.CostoUnitario).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Equipo>(e =>
            {
                e.HasOne(e => e.UsuarioAsignado)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioAsignadoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Ticket>(t =>
            {
                t.HasOne(t => t.CreadoPor)
                    .WithMany()
                    .HasForeignKey(t => t.CreadoPorId)
                    .OnDelete(DeleteBehavior.Restrict);

                t.HasOne(t => t.TecnicoAsignado)
                    .WithMany()
                    .HasForeignKey(t => t.TecnicoAsignadoId)
                    .OnDelete(DeleteBehavior.SetNull);

                t.HasOne(t => t.Equipo)
                    .WithMany()
                    .HasForeignKey(t => t.EquipoId)
                    .OnDelete(DeleteBehavior.SetNull);

                t.Property(t => t.NumeroSeguimiento).HasMaxLength(20);
            });

            //facturacion

            modelBuilder.Entity<Venta>(entity =>
            {
                entity.Property(e => e.Total).HasPrecision(18, 2);
                entity.Property(e => e.NumeroFactura).HasMaxLength(20);

                entity.HasOne(v => v.Paciente)
                      .WithMany()
                      .HasForeignKey(v => v.PacienteId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Usuario)
                      .WithMany()
                      .HasForeignKey(v => v.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.ValorClinico)
                      .WithMany()
                      .HasForeignKey(v => v.ValorClinicoId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(v => v.Sucursal)
                      .WithMany()
                      .HasForeignKey(v => v.SucursalId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Descuento).HasPrecision(18, 2);
                entity.Property(e => e.ReferenciaPago).HasMaxLength(100);
                entity.Property(e => e.RutaComprobante).HasMaxLength(512);

            });

            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);

                entity.HasOne(d => d.Venta)
                      .WithMany(v => v.Detalles)
                      .HasForeignKey(d => d.VentaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                      .WithMany()
                      .HasForeignKey(d => d.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });

            //RH

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(e => e.SalarioBase).HasPrecision(18, 2);
                entity.HasIndex(e => e.Cedula).IsUnique();  // ← Evita duplicados de cédula
            });

            modelBuilder.Entity<Planilla>(entity =>
            {
                entity.Property(e => e.SalarioOrdinario).HasPrecision(18, 2);
                entity.Property(e => e.ValorHorasExtras).HasPrecision(18, 2);
                entity.Property(e => e.ValorHorasDobles).HasPrecision(18, 2);
                entity.Property(e => e.ValorVacaciones).HasPrecision(18, 2);
                entity.Property(e => e.ValorIncapacidadParcial).HasPrecision(18, 2);
                entity.Property(e => e.ValorIncapacidadTotal).HasPrecision(18, 2);
                entity.Property(e => e.TotalIngresos).HasPrecision(18, 2);
                entity.Property(e => e.MontoCCSS).HasPrecision(18, 2);
                entity.Property(e => e.MontoImpuestoRenta).HasPrecision(18, 2);
                entity.Property(e => e.MontoSolidarista).HasPrecision(18, 2);
                entity.Property(e => e.TotalDeducciones).HasPrecision(18, 2);
                entity.Property(e => e.SalarioNeto).HasPrecision(18, 2);
                entity.Property(e => e.Comisiones).HasPrecision(18, 2);
                entity.Property(e => e.Prestamos).HasPrecision(18, 2);
                entity.Property(e => e.EmbargosPensiones).HasPrecision(18, 2);
                entity.Property(e => e.CuentasPorCobrar).HasPrecision(18, 2);
                entity.Property(e => e.AdelantoQuincena).HasPrecision(18, 2);
                entity.Property(e => e.PorcentajeCCSS).HasPrecision(5, 2);
                entity.Property(e => e.PorcentajeSolidarista).HasPrecision(5, 2);
            });

            //Permisos
            modelBuilder.Entity<Permiso>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Permiso>()
                .HasOne(p => p.AprobadoPor)
                .WithMany()
                .HasForeignKey(p => p.AprobadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Permiso>()
                .Property(p => p.RutaDocumentoIncapacidad)
                .HasMaxLength(512);

            //TecnologiaLente
            modelBuilder.Entity<TecnologiaLente>(entity =>
            {
                entity.Property(e => e.Precio).HasPrecision(18, 2);
            });

            //Aro
            modelBuilder.Entity<Aro>(entity =>
            {
                entity.HasIndex(a => a.SKU).IsUnique();
                entity.Property(a => a.Precio).HasPrecision(18, 2);
            });

        }





    }
}