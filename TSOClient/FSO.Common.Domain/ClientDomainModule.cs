using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using FSO.Common.Domain.Top100;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Common.Domain
{
    public static class ClientDomainModule
    {
        public static IServiceCollection AddClientDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IShardsDomain, ClientShards>();
            services.AddSingleton<IRealestateDomain, FSO.Common.Domain.Realestate.RealestateDomain>();
            services.AddSingleton<ITop100Domain, Top100Domain>();
            return services;
        }
    }
}
