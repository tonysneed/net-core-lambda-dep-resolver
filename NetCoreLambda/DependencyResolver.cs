using System;
using Microsoft.Extensions.DependencyInjection;

namespace NetCoreLambda
{
    public class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }

        public DependencyResolver(Action<IServiceCollection> registerServices)
        {
            // Set up Dependency Injection
            var services = new ServiceCollection();
            registerServices?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}