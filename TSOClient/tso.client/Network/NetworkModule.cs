using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Framework;
using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Server.Clients;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Client.Network
{
    public static class NetworkModule
    {
        public static IServiceCollection AddNetworkServices(this IServiceCollection services)
        {
            services.AddSingleton<AuthClient>(sp =>
            {
                var content = sp.Get<Content.Content>();
                if (GlobalSettings.Default.UseCustomServer)
                {
                    return new AuthClient(GlobalSettings.Default.GameEntryUrl);
                }
                else
                {
                    var authClientConfig = content.Ini.Get("gameentry.ini");
                    var serverAddress = authClientConfig["Auth"]["Server"];
                    if (serverAddress.IndexOf(",") != -1)
                    {
                        serverAddress = serverAddress.Substring(0, serverAddress.IndexOf(","));
                    }
                    if (serverAddress.IndexOf("://") == -1)
                    {
                        serverAddress = "https://" + serverAddress;
                    }
                    return new AuthClient(serverAddress);
                }
            });

            services.AddSingleton<CityClient>(sp =>
            {
                var content = sp.Get<Content.Content>();
                if (GlobalSettings.Default.UseCustomServer)
                {
                    return new CityClient(GlobalSettings.Default.CitySelectorUrl);
                }
                else
                {
                    var cityClientConfig = content.Ini.Get("cityselector.ini");
                    var serverAddress = cityClientConfig["CitySelector"]["ServerName"];
                    if (serverAddress.IndexOf(",") != -1)
                    {
                        serverAddress = serverAddress.Substring(0, serverAddress.IndexOf(","));
                    }
                    var port = int.Parse(cityClientConfig["CitySelector"]["ServerPort"]);
                    if (serverAddress.IndexOf("://") == -1)
                    {
                        if (port == 80)
                        {
                            serverAddress = "http://" + serverAddress;
                        }
                        else
                        {
                            serverAddress = "https://" + serverAddress;
                        }
                    }
                    return new CityClient(serverAddress);
                }
            });

            services.AddKeyedSingleton<AriesClient>("City", (sp, key) => new AriesClient(new FSOKernel(sp)));
            services.AddKeyedSingleton<AriesClient>("Lot", (sp, key) => new AriesClient(new FSOKernel(sp)));

            services.AddSingleton<cTSOSerializer>(sp =>
            {
                var content = sp.Get<Content.Content>();
                return new cTSOSerializer(content.DataDefinition);
            });

            services.AddSingleton<IModelSerializer>(sp =>
            {
                var content = sp.Get<Content.Content>();
                var serializer = new ModelSerializer();
                serializer.AddTypeSerializer(new DatabaseTypeSerializer());
                serializer.AddTypeSerializer(new DataServiceModelTypeSerializer(content.DataDefinition));
                serializer.AddTypeSerializer(new DataServiceModelVectorTypeSerializer(content.DataDefinition));
                return serializer;
            });

            services.AddTransient<ISerializationContext, SerializationContext>();

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IClientDataService, ClientDataService>();
            services.AddSingleton<Network>();

            return services;
        }
    }
}
