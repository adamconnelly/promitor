using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promitor.Agents.Core;
using Promitor.Agents.Core.Configuration.Server;
using Promitor.Agents.Scraper.Validation;
using Promitor.Core;
using Serilog;

namespace Promitor.Agents.Scraper
{
    public class Program : AgentProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                Welcome();

                // Let's hook in a logger for start-up purposes.
                ConfigureStartupLogging();

                var host = CreateHostBuilder(args)
                    .Build();
                
                using (var scope = host.Services.CreateScope())
                {
                    var validator = scope.ServiceProvider.GetRequiredService<RuntimeValidator>();
                    if (!validator.Run())
                    {
                        return (int)ExitStatus.ValidationFailed;
                    }
                }

                host.Run();

                return (int)ExitStatus.Success;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
                return (int)ExitStatus.UnhandledException;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void Welcome()
        {
            Console.WriteLine(Constants.Texts.Welcome);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfiguration configuration = BuildConfiguration();
            ServerConfiguration serverConfiguration = GetServerConfiguration(configuration);
            IHostBuilder webHostBuilder = CreatePromitorWebHost<Startup>(args, configuration, serverConfiguration);

            return webHostBuilder;
        }

        private static ServerConfiguration GetServerConfiguration(IConfiguration configuration)
        {
            var serverConfiguration = configuration.GetSection("server").Get<ServerConfiguration>();
            return serverConfiguration;
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var configurationFolder = Environment.GetEnvironmentVariable(EnvironmentVariables.Configuration.Folder);
            if (string.IsNullOrWhiteSpace(configurationFolder))
            {
                throw new Exception("Unable to determine the configuration folder");
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile($"{configurationFolder}/runtime.yaml", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(prefix: "PROMITOR_") // Used for all environment variables for Promitor
                .AddEnvironmentVariables(prefix: "PROMITOR_YAML_OVERRIDE_") // Used to overwrite runtime YAML
                .Build();

            return configuration;
        }
    }
}