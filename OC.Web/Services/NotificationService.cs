using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace OC.Web.Services
{
    /// <summary>Implementación de notificaciones de citas. Registra cada envío en EnviosNotificacion (fecha/hora). Canal Email/SMS/WhatsApp; por defecto se registra y se puede extender con envío real.</summary>
    public class NotificationService : INotificationService
    {
        private readonly IGenericRepository<EnvioNotificacion> _enviosRepo;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IGenericRepository<EnvioNotificacion> enviosRepo,
            ILogger<NotificationService> logger)
        {
            _enviosRepo = enviosRepo;
            _logger = logger;
        }

        public async Task EnviarRecordatorioPrevioAsync(Cita cita, int horasAntes, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeRecordatorio(cita, esInmediato: false);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.RecordatorioPrevio,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Recordatorio previo registrado para CitaId={CitaId}, PacienteId={PacienteId}, Canal={Canal}", cita.Id, cita.PacienteId, cita.CanalNotificacion);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Recordatorio de cita", ct);
        }

        public async Task EnviarRecordatorioInmediatoAsync(Cita cita, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeRecordatorio(cita, esInmediato: true);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.RecordatorioInmediato,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Recordatorio inmediato registrado para CitaId={CitaId}, PacienteId={PacienteId}", cita.Id, cita.PacienteId);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Cita agendada para hoy", ct);
        }

        public async Task EnviarNotificacionCancelacionAsync(Cita cita, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeCancelacion(cita);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.Cancelacion,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Notificación de cancelación registrada para CitaId={CitaId}, PacienteId={PacienteId}", cita.Id, cita.PacienteId);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Cita cancelada", ct);
        }

        public async Task<bool> NotificarLentesListosAsync(OrdenTrabajo orden, CancellationToken ct = default)
        {
            var paciente = orden.Paciente;
            if (paciente == null) return false;

            string? destinatario = null;
            string canal = "Email";
            if (!string.IsNullOrWhiteSpace(paciente.Email))
            {
                destinatario = paciente.Email;
                canal = "Email";
            }
            else if (!string.IsNullOrWhiteSpace(paciente.Telefono))
            {
                destinatario = paciente.Telefono;
                canal = "SMS";
            }

            var sede = orden.Sucursal?.Nombre ?? "nuestra sede";
            var mensaje = $"Sus lentes están listos para retiro en {sede}. Puede pasar a recogerlos cuando lo desee.";

            if (string.IsNullOrWhiteSpace(destinatario))
            {
                var errorMsg = "Paciente sin correo ni teléfono registrado. No se pudo enviar la notificación.";
                var registroError = new EnvioNotificacion
                {
                    OrdenTrabajoId = orden.Id,
                    CitaId = null,
                    TipoNotificacion = TipoNotificacionOrdenTrabajo.LentesListos,
                    FechaHoraEnvio = DateTime.Now,
                    Canal = "N/A",
                    Destinatario = null,
                    MensajeResumen = errorMsg,
                    Exito = false
                };
                await _enviosRepo.AddAsync(registroError);
                _logger.LogWarning("OT-HU-023: No se notificó OrdenTrabajoId={OrdenId}, PacienteId={PacienteId}: sin datos de contacto.", orden.Id, orden.PacienteId);
                return false;
            }

            var registro = new EnvioNotificacion
            {
                OrdenTrabajoId = orden.Id,
                CitaId = null,
                TipoNotificacion = TipoNotificacionOrdenTrabajo.LentesListos,
                FechaHoraEnvio = DateTime.Now,
                Canal = canal,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("OT-HU-023: Notificación lentes listos registrada para OrdenTrabajoId={OrdenId}, PacienteId={PacienteId}, Canal={Canal}", orden.Id, orden.PacienteId, canal);
            await EnviarPorCanalLentesAsync(destinatario, mensaje, "Sus lentes están listos", canal, ct);
            return true;
        }

        private static Task EnviarPorCanalLentesAsync(string destinatario, string mensaje, string asunto, string canal, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        private static (string? Destinatario, string MensajeResumen) ConstruirMensajeRecordatorio(Cita cita, bool esInmediato)
        {
            var lugar = cita.Sucursal?.Nombre ?? "Sede";
            var fechaHora = cita.FechaHora.ToString("dd/MM/yyyy HH:mm");
            var mensaje = esInmediato
                ? $"Su cita fue agendada para hoy. Fecha y hora: {fechaHora}. Lugar: {lugar}."
                : $"Recordatorio: Tiene una cita el {fechaHora} en {lugar}.";
            var destinatario = ObtenerDestinatario(cita);
            return (destinatario, mensaje);
        }

        private static (string? Destinatario, string MensajeResumen) ConstruirMensajeCancelacion(Cita cita)
        {
            var mensaje = "Su cita ha sido cancelada y ya no se realizará. Si desea reagendar, puede solicitar una nueva cita.";
            var destinatario = ObtenerDestinatario(cita);
            return (destinatario, mensaje);
        }

        private static string? ObtenerDestinatario(Cita cita)
        {
            var p = cita.Paciente;
            if (p == null) return null;
            return cita.CanalNotificacion switch
            {
                "Email" => p.Email,
                "SMS" or "WhatsApp" => p.Telefono,
                _ => p.Email ?? p.Telefono
            };
        }

        /// <summary>Punto de extensión: envío real por Email/SMS/WhatsApp. Por defecto solo se registró en DB.</summary>
        private static Task EnviarPorCanalAsync(Cita cita, string? destinatario, string mensaje, string asunto, CancellationToken ct)
        {
            // Aquí se puede integrar SMTP, Twilio, etc. Por ahora solo registro en EnviosNotificacion.
            return Task.CompletedTask;
        }
    }
}
