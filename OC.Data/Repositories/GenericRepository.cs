using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Common;
using OC.Core.Contracts.IRepositories;
using OC.Data.Context;
using System.Linq.Expressions;

namespace OC.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                var key = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
                if (key != null && key.Properties.Count == 1)
                {
                    var keyValue = entry.Property(key.Properties[0].Name).CurrentValue;
                    var tracked = _dbSet.Local
                        .FirstOrDefault(e => _context.Entry(e).Property(key.Properties[0].Name).CurrentValue?.Equals(keyValue) == true);
                    if (tracked != null)
                    {
                        _context.Entry(tracked).CurrentValues.SetValues(entity);
                        await _context.SaveChangesAsync();
                        return;
                    }
                }
            }

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        // LA JOYA DE LA CORONA: Paginación optimizada
        public async Task<PagedResult<T>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            // 1. Filtrar (WHERE)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Incluir relaciones (JOINS) - Ej: "Rol,Sucursal"
            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            // 3. Contar total real antes de paginar
            var totalCount = await query.CountAsync();

            // 4. Ordenar (ORDER BY)
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 5. Paginar (SKIP & TAKE) - Aquí se ve la mejora del rendimiento
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking() // Lectura rápida
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query
                .AsNoTracking()
                .ToListAsync();
        }
    }
}