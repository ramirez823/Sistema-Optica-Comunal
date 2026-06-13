using OC.Core.Domain.Entities;
using System;

namespace OC.Core.Services
{
    public static class SLAService
    {
        public static (int horasRespuesta, int horasResolucion) GetHorasSLA(string prioridad)
        {
            return prioridad switch
            {
                "Urgente" => (4, 24),
                "Alta" => (8, 48),
                "Media" => (16, 72),
                "Baja" => (24, 120),
                _ => (24, 72)
            };
        }

        public static (DateTime respuestaEsperada, DateTime resolucionEsperada) CalcularFechasSLA(string prioridad)
        {
            var ahora = DateTime.Now;
            var (horasRespuesta, horasResolucion) = GetHorasSLA(prioridad);
            return (ahora.AddHours(horasRespuesta), ahora.AddHours(horasResolucion));
        }

        public static SLATicketStatus ObtenerEstadoSLA(Ticket ticket)
        {
            if (string.IsNullOrEmpty(ticket.Prioridad))
                return SLATicketStatus.EnPlazo;

            if (ticket.FechaCierre.HasValue)
            {
                return ticket.SLA_CumplidoResolucion ? SLATicketStatus.Cumplido : SLATicketStatus.Incumplido;
            }

            if (ticket.TecnicoAsignadoId.HasValue && !ticket.FechaPrimeraRespuesta.HasValue)
            {
                if (!ticket.FechaRespuestaEsperada.HasValue)
                    return SLATicketStatus.EnPlazo;

                if (DateTime.Now > ticket.FechaRespuestaEsperada.Value)
                    return SLATicketStatus.RespuestaVencida;

                var horasRestantes = (ticket.FechaRespuestaEsperada.Value - DateTime.Now).TotalHours;
                if (horasRestantes <= 4 && horasRestantes > 0)
                    return SLATicketStatus.RespuestaPorVencer;

                return SLATicketStatus.EnPlazo;
            }

            if (!ticket.FechaResolucionEsperada.HasValue)
                return SLATicketStatus.EnPlazo;

            if (DateTime.Now > ticket.FechaResolucionEsperada.Value)
                return SLATicketStatus.ResolucionVencida;

            var horasRestantesResolucion = (ticket.FechaResolucionEsperada.Value - DateTime.Now).TotalHours;
            if (horasRestantesResolucion <= 4 && horasRestantesResolucion > 0)
                return SLATicketStatus.ResolucionPorVencer;

            return SLATicketStatus.EnPlazo;
        }

        public static string ObtenerTextoEstado(SLATicketStatus estado)
        {
            return estado switch
            {
                SLATicketStatus.Cumplido => "SLA cumplido",
                SLATicketStatus.Incumplido => "SLA incumplido",
                SLATicketStatus.RespuestaVencida => "Respuesta vencida",
                SLATicketStatus.RespuestaPorVencer => "Respuesta por vencer",
                SLATicketStatus.ResolucionVencida => "Resolución vencida",
                SLATicketStatus.ResolucionPorVencer => "Resolución por vencer",
                _ => "En plazo"
            };
        }

        public static string ObtenerClaseCSS(SLATicketStatus estado)
        {
            return estado switch
            {
                SLATicketStatus.Cumplido => "sla-success",
                SLATicketStatus.Incumplido => "sla-danger",
                SLATicketStatus.RespuestaVencida => "sla-danger",
                SLATicketStatus.RespuestaPorVencer => "sla-warning",
                SLATicketStatus.ResolucionVencida => "sla-danger",
                SLATicketStatus.ResolucionPorVencer => "sla-warning",
                _ => "sla-info"
            };
        }
    }

    public enum SLATicketStatus
    {
        EnPlazo,
        RespuestaPorVencer,
        RespuestaVencida,
        ResolucionPorVencer,
        ResolucionVencida,
        Cumplido,
        Incumplido
    }
}