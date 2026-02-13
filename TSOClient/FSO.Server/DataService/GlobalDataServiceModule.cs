using FSO.Common.DatabaseService.Framework;
using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Server.Protocol.Voltron.DataService;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server.DataService
{
    public static class GlobalDataServiceModule
    {
        public static IServiceCollection AddGlobalDataServices(this IServiceCollection services)
        {
            services.AddSingleton<cTSOSerializer>(sp =>
            {
                var content = Content.Content.Get();
                return new cTSOSerializer(content.DataDefinition);
            });

            services.AddSingleton<IModelSerializer>(sp =>
            {
                var content = Content.Content.Get();
                var serializer = new ModelSerializer();
                serializer.AddTypeSerializer(new DatabaseTypeSerializer());
                serializer.AddTypeSerializer(new DataServiceModelTypeSerializer(content.DataDefinition));
                serializer.AddTypeSerializer(new DataServiceModelVectorTypeSerializer(content.DataDefinition));
                return serializer;
            });

            services.AddTransient<ISerializationContext, SerializationContext>();
            return services;
        }
    }
}
