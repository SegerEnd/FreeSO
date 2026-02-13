using Microsoft.Extensions.DependencyInjection;

namespace FSO.Common.DependencyInjection
{
    public sealed class ServiceContainer : IDisposable
    {
        public IServiceProvider Provider { get; }
        public IKernel Kernel { get; }
        internal IServiceCollection Descriptors { get; }

        private ServiceContainer(IServiceCollection descriptors, IServiceProvider provider)
        {
            Descriptors = descriptors;
            Provider = provider;
            Kernel = new FSOKernel(provider);
        }

        public static ServiceContainer Build(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            ServiceContainer self = null;
            services.AddSingleton<ServiceContainer>(sp => self!);
            services.AddSingleton<IKernel>(sp => new FSOKernel(sp));
            self = new ServiceContainer(services, services.BuildServiceProvider());
            return self;
        }

        public ServiceContainer CreateChild(Action<IServiceCollection> configureChild)
        {
            IServiceCollection child = new ServiceCollection();
            foreach (var d in Descriptors)
            {
                if (d.Lifetime == ServiceLifetime.Singleton)
                {
                    var t = d.ServiceType;
                    child.Add(d.IsKeyedService
                        ? new ServiceDescriptor(t, d.ServiceKey, (sp, k) => Provider.GetRequiredKeyedService(t, k), ServiceLifetime.Singleton)
                        : new ServiceDescriptor(t, sp => Provider.GetRequiredService(t), ServiceLifetime.Singleton));
                }
                else child.Add(d);
            }
            configureChild(child);
            ServiceContainer cc = null;
            child.AddSingleton<ServiceContainer>(sp => cc!);
            child.AddSingleton<IKernel>(sp => new FSOKernel(sp));
            cc = new ServiceContainer(child, child.BuildServiceProvider());
            return cc;
        }

        public void Dispose() => (Provider as IDisposable)?.Dispose();
    }
}
