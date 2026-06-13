using OC.Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso especializado a Proveedores. Centraliza consultas que el
    /// repositorio genérico no cubre de forma limpia: listado paginado con
    /// búsqueda, conteo de pedidos por proveedor y validación de FK para
    /// evitar desactivar proveedores con pedidos activos.
    /// </summary>
    public interface IProveedorRepository
    {
        /// <summary>
        /// Devuelve proveedores activos, paginados y filtrados opcionalmente
        /// por un término de búsqueda sobre Nombre, Correo o NumeroTelefonico.
        /// </summary>
        Task<PagedResult<Domain.Entities.Proveedor>> GetPagedActivosAsync(
            int pageIndex,
            int pageSize,
            string? search = null);

        /// <summary>True si el proveedor tiene al menos un pedido con Activo=1.</summary>
        Task<bool> TienePedidosActivosAsync(int proveedorId);

        /// <summary>Conteo total de pedidos (activos e inactivos) del proveedor.</summary>
        Task<int> ContarPedidosAsync(int proveedorId);

        /// <summary>
        /// Para una lista de proveedorIds, devuelve un diccionario { proveedorId -> pedidosActivos }
        /// calculado en una sola query SQL con GROUP BY (evita N+1).
        /// </summary>
        Task<Dictionary<int, int>> GetConteoPedidosActivosAsync(IEnumerable<int> proveedorIds);
    }
}
