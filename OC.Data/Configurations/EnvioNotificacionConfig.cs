using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class EnvioNotificacionConfig : IEntityTypeConfiguration<EnvioNotificacion>
    {
        public void Configure(EntityTypeBuilder<EnvioNotificacion> builder)
        {
            builder.ToTable("EnviosNotificacion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TipoNotificacion).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Canal).HasMaxLength(20);
            builder.Property(x => x.Destinatario).HasMaxLength(256);
            builder.Property(x => x.MensajeResumen).HasMaxLength(500);

            builder.HasOne(x => x.Cita)
                .WithMany(c => c.EnviosNotificacion)
                .HasForeignKey(x => x.CitaId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(x => x.OrdenTrabajo)
                .WithMany(o => o.EnviosNotificacion)
                .HasForeignKey(x => x.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}
