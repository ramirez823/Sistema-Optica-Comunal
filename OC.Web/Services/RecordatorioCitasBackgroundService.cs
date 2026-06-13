using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using Microsoft.Extensions.Options;

namespace OC.Web.Services
{
    /// <summary>Escenario 1 CIT-RF-016: envía recordatorio previo X horas antes de la cita. Ejecuta cada IntervaloJobMinutos.</summary>
    public class RecordatorioCitasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RecordatorioCitasOptions _options;
        private readonly ILogger<RecordatorioCitasBackgroundService> _logger;

        public RecordatorioCitasBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<RecordatorioCitasOptions> options,
            ILogger<RecordatorioCitasBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecordatorioCitasBackgroundService iniciado. HorasAntes={Horas}, IntervaloMin={Min}", _options.HorasAntesRecordatorio, _options.IntervaloJobMinutos);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarRecordatoriosPreviosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en job de recordatorios de citas.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_options.IntervaloJobMinutos), stoppingToken);
            }
        }

        private async Task ProcesarRecordatoriosPreviosAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var citasRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Cita>>();
            var enviosRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<EnvioNotificacion>>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var ahora = DateTime.Now;
            var ventanaInicio = ahora.AddHours(_options.HorasAntesRecordatorio).AddMinutes(-_options.IntervaloJobMinutos);
            var ventanaFin = ahora.AddHours(_options.HorasAntesRecordatorio).AddMinutes(_options.IntervaloJobMinutos);

            var citasVigentes = await citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 500,
                filter: c => c.NotificacionesActivas
                    && c.Estado != EstadoCita.Cancelada
                    && c.FechaHora >= ventanaInicio
                    && c.FechaHora <= ventanaFin,
                includeProperties: "Paciente,Sucursal"
            );

            foreach (var cita in citasVigentes.Items)
            {
                if (ct.IsCancellationRequested) break;

                var yaEnviado = await enviosRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1,
                    filter: e => e.CitaId == cita.Id && e.TipoNotificacion == TipoNotificacionCita.RecordatorioPrevio
                );
                if (yaEnviado.Items.Any()) continue;

                await notificationService.EnviarRecordatorioPrevioAsync(cita, _options.HorasAntesRecordatorio, ct);
            }
        }
    }
}
