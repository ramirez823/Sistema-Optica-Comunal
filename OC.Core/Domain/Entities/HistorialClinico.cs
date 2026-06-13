using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.Core.Domain.Entities
{
    public class HistorialClinico
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }
        public Paciente Paciente { get; set; }

        public int CitaId { get; set; }
        public Cita Cita { get; set; }

        public DateTime FechaAtencion { get; set; }

        public string Diagnostico { get; set; }
        public string Tratamiento { get; set; }
        public string Observaciones { get; set; }

        public int? OptometristaId { get; set; }
    }

}
