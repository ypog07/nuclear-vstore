using Autofac.Extensions.DependencyInjection;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using NuClear.VStore.Configuration;

using Serilog;

namespace NuClear.VStore.Renderer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .ConfigureServices(services => services.AddAutofac())
                   .ConfigureAppConfiguration((hostingContext, config) =>
                                                  {
                                                      var env = hostingContext.HostingEnvironment;
                                                      config.UseDefaultConfiguration(env.ContentRootPath, env.EnvironmentName);
                                                  })
                   .UseStartup<Startup>()
                   .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
                   .Build();
    }
}