namespace OC.Web.Services
{
    /// <summary>Configuración de recordatorios de citas (CIT-RF-016).</summary>
    public class RecordatorioCitasOptions
    {
        public const string SectionName = "RecordatorioCitas";
        /// <summary>Horas antes de la cita para enviar el recordatorio previo (ej: 24).</summary>
        public int HorasAntesRecordatorio { get; set; } = 24;
        /// <summary>Intervalo en minutos del job en segundo plano que busca citas a recordar.</summary>
        public int IntervaloJobMinutos { get; set; } = 15;
    }
}
