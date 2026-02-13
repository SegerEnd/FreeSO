using FSO.Common.DataService;
using FSO.Server.DataService;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.Servers.Lot
{
    public static class LotServerModule
    {
        public static IServiceCollection AddLotServerServices(this IServiceCollection services)
        {
            services.AddSingleton<IDataService, NullDataService>();
            services.AddSingleton<IDataServiceSyncFactory, DataServiceSyncFactory>();
            return services;
        }
    }
}
