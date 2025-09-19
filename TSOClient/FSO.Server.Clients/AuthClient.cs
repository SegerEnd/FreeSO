using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Authorization;
using RestSharp;

namespace FSO.Server.Clients
{
    public class AuthClient : AbstractHttpClient
    {
        public AuthClient(string baseUrl) : base(baseUrl)
        {
        }

        public async Task<AuthResult> Authenticate(AuthRequest input)
        {
            var client = Client();

            var request = new RestRequest("AuthLogin")
                            .AddQueryParameter("username", input.Username)
                            .AddQueryParameter("password", input.Password)
                            .AddQueryParameter("serviceID", input.ServiceID)
                            .AddQueryParameter("version", input.Version)
                            .AddQueryParameter("clientid", input.ClientID);

            var response = await client.ExecuteAsync(request);

            var result = new AuthResult { Valid = false };

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var lines = response.Content.Split('\n');
                foreach (var line in lines)
                {
                    var components = line.Trim().Split(new char[] { '=' }, 2);
                    if (components.Length != 2) continue;

                    switch (components[0])
                    {
                        case "Valid":
                            result.Valid = Boolean.Parse(components[1]);
                            break;
                        case "Ticket":
                            result.Ticket = components[1];
                            break;
                        case "reasoncode":
                            result.ReasonCode = components[1];
                            break;
                        case "reasontext":
                            result.ReasonText = components[1];
                            break;
                        case "reasonurl":
                            result.ReasonURL = components[1];
                            break;
                    }
                }
            }
            else
            {
                result.ReasonCode = "36 301";
            }

            return result;
        }
    }
}
