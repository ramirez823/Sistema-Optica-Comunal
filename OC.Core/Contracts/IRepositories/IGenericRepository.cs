using OC.Core.Common;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OC.Core.Contracts.IRepositories
{
    public interface IGenericRepository<T> where T : class
    {
        // Obtener por ID
        Task<T?> GetByIdAsync(int id);

        // Comandos
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);

        // CONSULTA MAESTRA PAGINADA
        // filter: x => x.Nombre == "Juan" (Filtra en SQL)
        // orderBy: q => q.OrderByDescending(x => x.Fecha)
        // includeProperties: "Pedidos,Detalles" (Joins)

        Task<PagedResult<T>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "");

        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null);
    }
}