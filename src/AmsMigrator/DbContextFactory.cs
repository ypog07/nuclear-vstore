using System;

using AmsMigrator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AmsMigrator
{
    public class DbContextFactory : IDbContextFactory
    {
        ImportOptions _options;
        public DbContextFactory(ImportOptions options)
        {
            _options = options;
        }

        public ErmContext GetNewContext()
        {
            var contextOptions = new DbContextOptionsBuilder<ErmContext>()
                                 .UseLoggerFactory(new LoggerFactory().AddSerilog())
                                 .UseSqlServer(_options.SourceDbConnectionString,
                                               sqlServerOptionsAction: sqlOptions =>
                                                                           {
                                                                               sqlOptions.EnableRetryOnFailure(
                                                                                   maxRetryCount: 5,
                                                                                   maxRetryDelay: TimeSpan.FromSeconds(30),
                                                                                   errorNumbersToAdd: null);
                                                                           })
                                 .Options;
            return new ErmContext(contextOptions);
        }
    }
}
