using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class ValorClinicoRepository : IValorClinicoRepository
    {
        private readonly AppDbContext _context;

        public ValorClinicoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Expediente?> GetExpedienteConValoresOrdenadosAsync(int expedienteId)
        {
            // EF Core 8 traduce los OrderBy dentro de Include a ORDER BY en el JOIN,
            // por lo que la ordenación ocurre en SQL y no en memoria.
            return await _context.Expedientes
                .AsNoTracking()
                .Include(e => e.Cita)
                    .ThenInclude(c => c.Paciente)
                .Include(e => e.ValoresClinicos.OrderByDescending(v => v.FechaRegistro))
                .Include(e => e.Documentos.OrderByDescending(d => d.FechaSubida))
                .FirstOrDefaultAsync(e => e.Id == expedienteId);
        }
    }
}
