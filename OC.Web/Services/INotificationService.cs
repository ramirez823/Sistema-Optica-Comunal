using OC.Core.Domain.Entities;

namespace OC.Web.Services
{
    /// <summary>Servicio de notificaciones de citas (CIT-RF-016). Registra cada envío con fecha y hora.</summary>
    public interface INotificationService
    {
        /// <summary>Envía recordatorio previo X horas antes de la cita. Registra fecha/hora de envío.</summary>
        Task EnviarRecordatorioPrevioAsync(Cita cita, int horasAntes, CancellationToken ct = default);

        /// <summary>Envía recordatorio inmediato para citas creadas el mismo día con poca anticipación. Registra fecha/hora de envío.</summary>
        Task EnviarRecordatorioInmediatoAsync(Cita cita, CancellationToken ct = default);

        /// <summary>Notifica al paciente que la cita fue cancelada. Registra fecha/hora de envío.</summary>
        Task EnviarNotificacionCancelacionAsync(Cita cita, CancellationToken ct = default);

        /// <summary>OT-HU-023: Notifica al paciente que sus lentes están listos. Retorna false si el paciente no tiene correo ni teléfono (registra el error).</summary>
        Task<bool> NotificarLentesListosAsync(OrdenTrabajo orden, CancellationToken ct = default);
    }
}
