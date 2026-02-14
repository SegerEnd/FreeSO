#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Common.DependencyInjection
{
    public sealed class ServiceContainer : IDisposable
    {
        public IServiceProvider Provider { get; }
        public IKernel Kernel { get; }

        private ServiceContainer(IServiceProvider provider)
        {
            Provider = provider;
            Kernel = new FSOKernel(provider);
        }

        public static ServiceContainer Build(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);

            ServiceContainer self = null!;
            services.AddSingleton<ServiceContainer>(sp => self);
            services.AddSingleton<IKernel>(sp => self.Kernel);

            self = new ServiceContainer(services.BuildServiceProvider());
            return self;
        }

        public void Dispose()
        {
            (Provider as IDisposable)?.Dispose();
        }
    }
}
