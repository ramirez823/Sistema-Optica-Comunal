using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    public class Asistencia
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime? HoraEntrada { get; set; }

        public DateTime? HoraSalida { get; set; }

        public bool Activo { get; set; } = true;
    }
}
