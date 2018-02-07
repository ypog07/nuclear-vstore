using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System;
using System.Linq;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

using AmsMigrator.ImportStrategies;

using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using CommandLine;
using System.Diagnostics;

using AmsMigrator.CLI;
using AmsMigrator.Infrastructure;

using Serilog.Context;

namespace AmsMigrator
{
    class Program
    {
        private static readonly IReadOnlyDictionary<string, (Language DefaultLang, int CountryCode, bool MigrateModerationStatuses)> InstanceMap = new Dictionary<string, (Language, int, bool)>
        {
            { "ErmRu", (Language.Ru, 1, true) },
            { "ErmUa", (Language.Ru, 11, false) },
            { "ErmAe", (Language.En, 14, false) },
            { "ErmCl", (Language.Es, 20, false) },
            { "ErmCy", (Language.En, 19, false) },
            { "ErmCz", (Language.Cs, 18, false) },
            { "ErmKg", (Language.Ru, 23, false) },
            { "ErmKz", (Language.Ru, 4, false) }
        };

        private static IConfigurationRoot Configuration { get; set; }

        static async Task Main(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("AMS_ENVIRONMENT") ?? "development";
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.ToLower()}.json")
                .AddEnvironmentVariables("AMS_")
                .Build();

            var opt = new ImportOptions();
            Configuration.GetSection("MigrationOptions").Bind(opt);

            if (args.Any())
            {
                var parsingCode = Parser.Default.ParseArguments<ImportByUuidsOptions, ResumeOptions>(args)
                    .MapResult(
                          (ImportByUuidsOptions opts) =>
                    {
                        try
                        {
                            if (opts.AdvertisementUuids != null && opts.InstanceKey == null)
                                throw new ArgumentNullException();

                            opt.AmsV1Uuids = opts.AdvertisementUuids.Split(new[] { ',' })
                                .Select(s => s.Trim())
                                .Select(Guid.Parse)
                                .Distinct()
                                .Select(s => s.ToString())
                                .ToArray();
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Invalid command-line arguments");
                            return 1;
                        }

                        opt.SourceDbConnectionString = Configuration.GetConnectionString($"{opts.InstanceKey}Connection");
                        opt.Language = InstanceMap[opts.InstanceKey].DefaultLang.ToString().ToLowerInvariant();
                        return 0;
                    },
                    (ResumeOptions opts) =>
                        {
                            opt.StartAfterFirmId = opts.StartFromFirmId;
                            opt.StartFromDb = opts.InstanceKey;
                            return 0;
                        },
                    errs => 1);

                if (parsingCode != 0)
                {
                    return;
                }
            }


            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(opt);
            services.AddSingleton<IDbContextFactory, DbContextFactory>();
            services.AddSingleton<IOkapiClient, OkapiClient>();
            services.AddSingleton<IAmsClient, AmsClient>();

            services.AddSingleton<IErmDbClient, ErmDbClient>();

            services.AddTransient<AdvertisementMaterialsImporter>();
            services.AddTransient<CompanyLogoImportStrategy>();
            services.AddTransient<ZmkLogoImportStrategy>();
            services.AddTransient<KBLogoImportStrategy>();

            IServiceProvider sp = services.BuildServiceProvider();
            services.AddSingleton(sp);
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Logger.Information("Configuration values: {settings}", JsonConvert.SerializeObject(opt));

            if (opt.AmsV1Uuids != null)
            {
                var importer = sp.GetService<AdvertisementMaterialsImporter>();

                Log.Logger.Information("Start import from command line argument");

                await importer.StartImportAsync(opt.AmsV1Uuids);

                return;
            }

            var sw = new Stopwatch();
            sw.Start();
            foreach (var instance in InstanceMap)
            {
                if (!string.IsNullOrEmpty(opt.StartFromDb) && !instance.Key.Equals(opt.StartFromDb, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Logger.Information("Starting from {instanceToStart}, skip instance {instance} from import", opt.StartFromDb, instance.Key);
                    continue;
                }

                var connectionString = Configuration.GetConnectionString($"{instance.Key}Connection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Log.Logger.Information("Connection string for {instance} not found, skip this instance from import", instance.Key);
                    continue;
                }

                opt.SourceDbConnectionString = connectionString;
                opt.DbInstanceKey = instance.Key;
                opt.Language = instance.Value.DefaultLang.ToString().ToLowerInvariant();
                var importer = sp.GetService<AdvertisementMaterialsImporter>();

                Log.Logger.Information("Start import from {instance} in mode: {mode}", instance.Key, opt.Targets.ToString());

                var p = new Progress<double>(n => Log.Logger.Information("Instance {instance}: {n:0.00}% processed.", instance.Key, n));
                using (LogContext.PushProperty("InstanceKey", instance.Key))
                {
                    var imported = await importer.StartImportAsync(p);
                    Stats.Collector[instance.Key] = imported;
                }
            }

            foreach (var kvp in Stats.Collector)
            {
                if (kvp.Key.StartsWith("Erm"))
                {
                    Log.Logger.Information("{0} materials processed from instance: {1}", kvp.Value, kvp.Key);
                }
            }
            sw.Stop();

            Log.Logger.Information("Overall migration execution time {time}", sw.Elapsed);
        }
    }
}
