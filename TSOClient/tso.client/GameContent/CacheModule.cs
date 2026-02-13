using FSO.Common.Utils.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Client.GameContent
{
    public static class CacheModule
    {
        public static IServiceCollection AddCacheServices(this IServiceCollection services)
        {
            services.AddSingleton<ICache>(sp =>
            {
                return new FileSystemCache("./fso_cache", 10 * 1024 * 1024);
            });
            return services;
        }
    }
}
