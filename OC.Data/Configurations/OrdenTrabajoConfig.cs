using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class OrdenTrabajoConfig : IEntityTypeConfiguration<OrdenTrabajo>
    {
        public void Configure(EntityTypeBuilder<OrdenTrabajo> builder)
        {
            builder.ToTable("OrdenesTrabajo");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Estado).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Referencia).HasMaxLength(200);
            builder.Property(x => x.PD).HasPrecision(4, 1);
            builder.Property(x => x.TipoLente).HasMaxLength(100);
            builder.Property(x => x.MaterialLente).HasMaxLength(100);
            builder.Property(x => x.Tratamientos).HasMaxLength(500);
            builder.Property(x => x.LaboratorioExterno).HasMaxLength(200);

            builder.HasOne(x => x.Paciente)
                .WithMany()
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Venta)
                .WithMany()
                .HasForeignKey(x => x.VentaId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
