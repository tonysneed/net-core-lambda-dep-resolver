using System;
using System.IO;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace NetCoreLambda
{
    public class Function
    {
        // Configuration
        private IConfiguration Configuration { get; }

        public Function()
        {
            // Set up dependency resolution
            var resolver = new DependencyResolver(ConfigureServices);

            // Get Configuration from DI system
            Configuration = resolver.ServiceProvider.GetService<IConfiguration>();
        }

        // Use this ctor from unit tests that can mock IConfiguration
        public Function(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// A simple function that takes a config key and returns a value.
        /// </summary>
        /// <param name="input">Configuration key</param>
        /// <param name="context">ILambdaContext</param>
        /// <returns>Configuration value</returns>
        public string FunctionHandler(string input, ILambdaContext context)
        {
            // Get config setting using input as a key
            return Configuration[input] ?? "None";
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add Configuration to DI system
            services.AddConfiguration(builder =>
            {
                // Get ASPNETCORE_ENVIRONMENT
                var environment = Environment.GetEnvironmentVariable(
                    Constants.EnvironmentVariables.AspnetCoreEnvironment)
                    ?? Constants.Environments.Production;

                // Build config
                return builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environment}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
            });
        }
    }
}
