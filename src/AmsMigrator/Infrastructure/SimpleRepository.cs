using System;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace AmsMigrator.Infrastructure
{
    public class SimpleRepository<TEntity> : IDisposable where TEntity : class
    {
        private DbContext _context;
        private bool _contextDisposed;

        public SimpleRepository(DbContext context)
        {
            _context = context;
        }

        private DbSet<TEntity> Entities =>
            _context.Set<TEntity>();

        public void Add(TEntity entity)
        {
            Entities.Add(entity);
            _context.Entry(entity).State = EntityState.Added;
        }

        public void Edit(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public async Task CommitChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            if (!_contextDisposed)
            {
                _context.Dispose();
                _contextDisposed = true;
            }
        }
    }
}
