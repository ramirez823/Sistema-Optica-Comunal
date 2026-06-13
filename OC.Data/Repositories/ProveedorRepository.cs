using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Common;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly AppDbContext _context;

        public ProveedorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Proveedor>> GetPagedActivosAsync(
            int pageIndex,
            int pageSize,
            string? search = null)
        {
            IQueryable<Proveedor> query = _context.Proveedores
                .AsNoTracking()
                .Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(term) ||
                    p.Correo.ToLower().Contains(term) ||
                    p.NumeroTelefonico.ToLower().Contains(term));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Nombre)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Proveedor>(items, total, pageIndex, pageSize);
        }

        public async Task<bool> TienePedidosActivosAsync(int proveedorId)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .AnyAsync(p => p.ProveedorId == proveedorId && p.Activo);
        }

        public async Task<int> ContarPedidosAsync(int proveedorId)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .CountAsync(p => p.ProveedorId == proveedorId);
        }

        public async Task<Dictionary<int, int>> GetConteoPedidosActivosAsync(IEnumerable<int> proveedorIds)
        {
            var ids = proveedorIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            // Una sola query con GROUP BY: SELECT ProveedorId, COUNT(*) FROM Pedidos
            // WHERE Activo=1 AND ProveedorId IN (...) GROUP BY ProveedorId
            var rows = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Activo && ids.Contains(p.ProveedorId))
                .GroupBy(p => p.ProveedorId)
                .Select(g => new { ProveedorId = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(r => r.ProveedorId, r => r.Count);
        }
    }
}
