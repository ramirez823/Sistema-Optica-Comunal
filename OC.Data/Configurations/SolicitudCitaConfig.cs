using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class SolicitudCitaConfig : IEntityTypeConfiguration<SolicitudCita>
    {
        public void Configure(EntityTypeBuilder<SolicitudCita> builder)
        {
            builder.ToTable("SolicitudesCitas");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Motivo)
                .HasMaxLength(500);

            builder.Property(x => x.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");

            // Relación con Paciente
            builder.HasOne(x => x.Paciente)
                .WithMany(p => p.SolicitudesCitas)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Usuario (Recepcionista que aprueba)
            builder.HasOne(x => x.UsuarioAprobador)
                .WithMany()
                .HasForeignKey(x => x.UsuarioAprobadorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
