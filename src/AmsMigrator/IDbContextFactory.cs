using AmsMigrator.Models;

namespace AmsMigrator
{
    public interface IDbContextFactory
    {
        ErmContext GetNewContext();
    }
}