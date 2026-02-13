using Microsoft.Extensions.DependencyInjection;

namespace FSO.Client.Regulators
{
    public static class RegulatorsModule
    {
        public static IServiceCollection AddRegulators(this IServiceCollection services)
        {
            services.AddSingleton<LoginRegulator>();
            services.AddSingleton<CityConnectionRegulator>();
            services.AddSingleton<CreateASimRegulator>();
            services.AddSingleton<PurchaseLotRegulator>();
            services.AddSingleton<LotConnectionRegulator>();
            return services;
        }
    }
}
