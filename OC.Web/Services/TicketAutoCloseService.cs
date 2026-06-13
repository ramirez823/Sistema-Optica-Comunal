using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OC.Web.Services
{
    public class TicketAutoCloseService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketAutoCloseService> _logger;

        public TicketAutoCloseService(IServiceProvider serviceProvider, ILogger<TicketAutoCloseService> logger)
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
                    await CerrarTicketsVencidos();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar tickets vencidos");
                }

                // Ejecutar una vez al día
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task CerrarTicketsVencidos()
        {
            using var scope = _serviceProvider.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Ticket>>();

            var fechaLimite = DateTime.Now.AddDays(-7);
            var ticketsVencidos = await ticketRepo.GetPagedAsync(1, 1000, filter: t =>
                t.Estado == "Resuelto" && t.FechaResolucion <= fechaLimite);

            foreach (var ticket in ticketsVencidos.Items)
            {
                ticket.Estado = "Cerrado";
                ticket.FechaCierre = DateTime.Now;
                ticket.CalificacionCliente = 3;
                ticket.ComentarioCliente = "Cerrado automáticamente por falta de calificación.";
                ticket.FechaCalificacion = DateTime.Now;

                // Calcular cumplimiento de SLA de resolución
                if (ticket.FechaResolucionEsperada.HasValue)
                {
                    ticket.SLA_CumplidoResolucion = ticket.FechaCierre <= ticket.FechaResolucionEsperada;
                }

                await ticketRepo.UpdateAsync(ticket);
                _logger.LogInformation($"Ticket {ticket.NumeroSeguimiento} cerrado automáticamente por expiración de calificación.");
            }
        }
    }
}