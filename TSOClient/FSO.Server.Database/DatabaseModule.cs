using FSO.Server.Database.DA;
using FSO.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.Database
{
    public static class DatabaseModule
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            services.AddSingleton<IDAFactory>(sp =>
            {
                var config = sp.Get<DatabaseConfiguration>();
                return config.Engine switch
                {
                    "mysql" => new MySqlDAFactory(config),
                    "sqlite" => new SqliteDAFactory(config),
                    _ => throw new NotSupportedException($"Unsupported database engine {config.Engine}")
                };
            });
            return services;
        }
    }
}
