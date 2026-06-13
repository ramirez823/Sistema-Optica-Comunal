using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);

            builder.Property(p => p.Correo).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Correo).IsUnique(); // Correo único en el sistema

            builder.Property(p => p.Contrasena).IsRequired().HasMaxLength(255);

            // Relaciones explícitas (Buenas prácticas)
            builder.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict); // Si se borra un Rol, no se borra al usuario (seguridad)

            builder.HasOne(u => u.Sucursal)
                .WithMany(s => s.Usuarios)
                .HasForeignKey(u => u.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.Cedula).HasMaxLength(9);
            builder.Property(u => u.Banco).HasMaxLength(100);
            builder.Property(u => u.NumeroCuentaIBAN).HasMaxLength(22);
            builder.Property(u => u.TotpSecretProtegido).HasMaxLength(1024);
            builder.Property(u => u.TokenRecuperacion).HasMaxLength(64);
            builder.Property(u => u.DebeCambiarContrasena).HasDefaultValue(false);
            builder.Property(u => u.TotpHabilitado).HasDefaultValue(false);
        }
    }
}