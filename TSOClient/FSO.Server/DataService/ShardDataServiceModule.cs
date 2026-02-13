using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.DataService
{
    public static class ShardDataServiceModule
    {
        public static IServiceCollection AddShardDataServices(this IServiceCollection services, string simNFS)
        {
            services.AddSingleton<IServerNFSProvider>(new ServerNFSProvider(simNFS));
            services.AddSingleton<IDataService, ServerDataService>();
            return services;
        }
    }
}
