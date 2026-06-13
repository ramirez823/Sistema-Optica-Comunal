using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Core.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OC.Web.Services
{
    public class SLAMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SLAMonitorService> _logger;

        public SLAMonitorService(IServiceProvider serviceProvider, ILogger<SLAMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarSLA();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al verificar SLA");
                }

                // Ejecutar cada hora
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task VerificarSLA()
        {
            using var scope = _serviceProvider.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Ticket>>();
            //var emailService = scope.ServiceProvider.GetService<IEmailService>(); // Opcional

            var ahora = DateTime.Now;

            // Obtener tickets activos (no cerrados)
            var ticketsActivos = await ticketRepo.GetPagedAsync(1, 1000, filter: t =>
                t.Estado != "Cerrado" &&
                t.Prioridad != null &&
                t.FechaRespuestaEsperada.HasValue);

            foreach (var ticket in ticketsActivos.Items)
            {
                var estado = SLAService.ObtenerEstadoSLA(ticket);
                bool debeAlertar = false;
                string alerta = "";

                switch (estado)
                {
                    case SLATicketStatus.RespuestaPorVencer when !ticket.FechaUltimaAlertaSLA.HasValue:
                        debeAlertar = true;
                        alerta = $"⚠️ Alerta: El ticket {ticket.NumeroSeguimiento} tiene {CalcularHorasRestantes(ticket.FechaRespuestaEsperada.Value)} horas para ser respondido.";
                        break;

                    case SLATicketStatus.RespuestaVencida when !ticket.SLA_CumplidoRespuesta:
                        debeAlertar = true;
                        ticket.SLA_CumplidoRespuesta = false;
                        ticket.SLA_Observacion = $"Respuesta vencida desde {ticket.FechaRespuestaEsperada.Value:dd/MM/yyyy HH:mm}";
                        alerta = $"🚨 INCUMPLIMIENTO SLA: El ticket {ticket.NumeroSeguimiento} no fue respondido a tiempo.";
                        break;

                    case SLATicketStatus.ResolucionPorVencer when !ticket.FechaUltimaAlertaSLA.HasValue:
                        debeAlertar = true;
                        alerta = $"⚠️ Alerta: El ticket {ticket.NumeroSeguimiento} tiene {CalcularHorasRestantes(ticket.FechaResolucionEsperada.Value)} horas para ser resuelto.";
                        break;

                    case SLATicketStatus.ResolucionVencida when !ticket.SLA_CumplidoResolucion:
                        debeAlertar = true;
                        ticket.SLA_CumplidoResolucion = false;
                        ticket.SLA_Observacion = $"Resolución vencida desde {ticket.FechaResolucionEsperada.Value:dd/MM/yyyy HH:mm}";
                        alerta = $"🚨 INCUMPLIMIENTO SLA: El ticket {ticket.NumeroSeguimiento} no fue resuelto a tiempo.";
                        break;
                }

                if (debeAlertar)
                {
                    ticket.FechaUltimaAlertaSLA = ahora;
                    await ticketRepo.UpdateAsync(ticket);

                    if (!string.IsNullOrEmpty(alerta))
                    {
                        _logger.LogWarning(alerta);

                        // Enviar email al técnico asignado si existe
                       // if (emailService != null && ticket.TecnicoAsignadoId.HasValue)
                        {
                            // Aquí puedes implementar envío de email
                            // await emailService.EnviarAlertaSLA(ticket, alerta);
                        }
                    }
                }
            }
        }

        private double CalcularHorasRestantes(DateTime fechaLimite)
        {
            var horas = (fechaLimite - DateTime.Now).TotalHours;
            return Math.Max(0, Math.Round(horas, 1));
        }
    }
}