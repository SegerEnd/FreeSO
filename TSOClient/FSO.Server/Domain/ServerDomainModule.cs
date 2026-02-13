using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.Domain
{
    public static class ServerDomainModule
    {
        public static IServiceCollection AddServerDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IShardsDomain, Shards>();
            services.AddSingleton<IRealestateDomain, RealestateDomain>();
            return services;
        }
    }
}
