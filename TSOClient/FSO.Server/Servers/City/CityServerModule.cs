using FSO.Common.Domain.Top100;
using FSO.Server.Domain;
using FSO.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.Servers.City
{
    public static class CityServerModule
    {
        public static IServiceCollection AddCityServerServices(this IServiceCollection services)
        {
            services.AddSingleton<ServerTop100Domain>();
            services.AddTransient<ITop100Domain>(sp => sp.Get<ServerTop100Domain>());
            return services;
        }
    }
}
