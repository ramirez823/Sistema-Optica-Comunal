using System.Threading.Tasks;
using OC.Core.Domain.Entities;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso de lectura especializado para el detalle del Expediente clínico.
    /// Centraliza el ORDER BY de las colecciones hijas (ValoresClinicos, Documentos)
    /// a nivel de SQL para evitar la ordenación en memoria del servidor web.
    /// </summary>
    public interface IValorClinicoRepository
    {
        /// <summary>
        /// Devuelve el Expediente con sus colecciones hijas ya ordenadas en la base de datos:
        /// ValoresClinicos por FechaRegistro DESC y Documentos por FechaSubida DESC.
        /// </summary>
        Task<Expediente?> GetExpedienteConValoresOrdenadosAsync(int expedienteId);
    }
}
