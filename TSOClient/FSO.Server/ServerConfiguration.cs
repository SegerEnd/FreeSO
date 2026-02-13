using FSO.Common;
using FSO.Server.Database;
using FSO.Server.Discord;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Servers.City;
using FSO.Server.Servers.Lot;
using FSO.Server.Servers.Tasks;
using FSO.Server.Servers.UserApi;
using FSO.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Server
{
    public class ServerConfiguration
    {
        [JsonProperty("gameLocation")]
        public string GameLocation;
        [JsonProperty("simNFS")]
        public string SimNFS;
        [JsonProperty("updateBranch")]
        public string UpdateBranch;

        [JsonProperty("allOpenable")]
        public bool AllOpenable;

        [JsonProperty("archive")]
        public ArchiveConfiguration Archive; // If this is present, the server is running in archive mode

        [JsonProperty("database")]
        public DatabaseConfiguration Database;
        [JsonProperty("services")]
        public ServerConfigurationservices Services;
        [JsonProperty("discord")]
        public DiscordConfiguration Discord;

        /// <summary>
        /// Secret string used as a key for signing JWT tokens for the admin system
        /// </summary>
        [JsonProperty("secret")]
        public string Secret;

        /// <summary>
        /// Update ID this server is running on. All shards that we host will report needing this version, and this is reported with our host information.
        /// Loaded from updateID.txt if present.
        /// </summary>
        [JsonProperty("updateID")]
        public int? UpdateID;

        [JsonProperty("events")]
        public EventConfig? Events; // If this is present, the server automatically schedules events on start.
    }


    public class ServerConfigurationservices
    {
        [JsonProperty("userApi")]
        public ApiServerConfiguration UserApi;
        [JsonProperty("tasks")]
        public TaskServerConfiguration Tasks;
        [JsonProperty("cities")]
        public List<CityServerConfiguration> Cities;
        [JsonProperty("lots")]
        public List<LotServerConfiguration> Lots;
    }

    public static class ServerConfigurationModule
    {
        private static ServerConfiguration LoadConfiguration(ServerConfiguration explicitConfig)
        {
            if (explicitConfig != null)
            {
                return explicitConfig;
            }

            var configPath = "config.json";
            if (!File.Exists(configPath))
            {
                throw new Exception("Configuration file, config.json, missing");
            }

            var data = File.ReadAllText(configPath);

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfiguration>(data);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not deserialize config.json", ex);
            }
        }

        public static IServiceCollection AddServerConfiguration(this IServiceCollection services, ServerConfiguration explicitConfig = null)
        {
            services.AddSingleton<ServerConfiguration>(sp => LoadConfiguration(explicitConfig));
            services.AddSingleton<DatabaseConfiguration>(sp => sp.Get<ServerConfiguration>().Database);
            services.AddSingleton<JWTConfiguration>(sp => new JWTConfiguration()
            {
                Key = System.Text.UTF8Encoding.UTF8.GetBytes(sp.Get<ServerConfiguration>().Secret)
            });
            return services;
        }
    }
}
