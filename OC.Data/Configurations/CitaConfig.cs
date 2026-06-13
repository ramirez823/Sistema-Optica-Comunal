using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class CitaConfig : IEntityTypeConfiguration<Cita>
    {
        public void Configure(EntityTypeBuilder<Cita> builder)
        {
            builder.ToTable("Citas");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MotivoConsulta)
                .HasMaxLength(1000);

            builder.Property(x => x.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Programada");

            builder.Property(x => x.NotificacionesActivas)
                .HasDefaultValue(true);

            builder.Property(x => x.CanalNotificacion)
                .HasMaxLength(20)
                .HasDefaultValue("Email");

            // Relación con Paciente
            builder.HasOne(x => x.Paciente)
                .WithMany(p => p.Citas)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con SolicitudCita
            builder.HasOne(x => x.SolicitudCita)
                .WithMany()
                .HasForeignKey(x => x.SolicitudCitaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Usuario (Optometrista asignado)
            builder.HasOne(x => x.UsuarioAsignado)
                .WithMany()
                .HasForeignKey(x => x.UsuarioAsignadoId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
