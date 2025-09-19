using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.CitySelector;
using RestSharp;

namespace FSO.Server.Clients
{
    public class CityClient : AbstractHttpClient
    {
        public CityClient(string baseUrl) : base(baseUrl) { }

        public async Task<ShardSelectorServletResponse> ShardSelectorServletAsync(ShardSelectorServletRequest input)
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/ShardSelectorServlet")
                .AddQueryParameter("shardName", input.ShardName)
                .AddQueryParameter("avatarId", input.AvatarID.ToString());

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Unknown error during ShardSelectorServlet");

            return XMLUtils.Parse<ShardSelectorServletResponse>(response.Content);
        }

        public async Task<InitialConnectServletResult> InitialConnectServletAsync(InitialConnectServletRequest input)
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/InitialConnectServlet")
                .AddQueryParameter("ticket", input.Ticket)
                .AddQueryParameter("version", input.Version);

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Unknown error during InitialConnectServlet");

            return XMLUtils.Parse<InitialConnectServletResult>(response.Content);
        }

        public async Task<List<AvatarData>> AvatarDataServletAsync()
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/AvatarDataServlet");

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Unknown error during AvatarDataServlet");

            var aotDummy = new XMLList<AvatarData>();
            return (List<AvatarData>)XMLUtils.Parse<XMLList<AvatarData>>(response.Content);
        }

        public async Task<List<ShardStatusItem>> ShardStatusAsync()
        {
            var client = Client();

            var request = new RestRequest("cityselector/shard-status.jsp");

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Unknown error during ShardStatus");

            var aotDummy = new XMLList<ShardStatusItem>();
            return (List<ShardStatusItem>)XMLUtils.Parse<XMLList<ShardStatusItem>>(response.Content);
        }
    }
}
