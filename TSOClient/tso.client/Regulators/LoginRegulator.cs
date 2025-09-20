using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Domain.Shards;
using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Authorization;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Client.Regulators
{
    /// <summary>
    /// Handles authentication and city server network activity
    /// </summary>
    public class LoginRegulator : AbstractRegulator
    {
        public AuthResult AuthResult { get; internal set; }
        public List<AvatarData> Avatars { get; internal set; } = new List<AvatarData>();
        public IShardsDomain Shards;

        private AuthClient AuthClient;
        private CityClient CityClient;

        public LoginRegulator(AuthClient authClient, CityClient cityClient, IShardsDomain domain)
        {
            this.Shards = domain;
            this.AuthClient = authClient;
            this.CityClient = cityClient;

            AddState("NotLoggedIn")
                .Default()
                    .Transition()
                        .OnData(typeof(AuthRequest)).TransitionTo("AuthLogin");

            AddState("AuthLogin").OnlyTransitionFrom("NotLoggedIn");
            AddState("InitialConnect").OnlyTransitionFrom("AuthLogin");
            AddState("AvatarData").OnlyTransitionFrom("InitialConnect", "UpdateRequired", "LoggedIn");
            AddState("ShardStatus").OnlyTransitionFrom("AvatarData");
            AddState("LoggedIn").OnlyTransitionFrom("ShardStatus");

            AddState("UpdateRequired").OnlyTransitionFrom("InitialConnect");
        }

        protected override async void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "AuthLogin":
                    var loginData = (AuthRequest)data;
                    AuthResult result = null;
                    try
                    {
                        result = await AuthClient.Authenticate(loginData); // Await the async method
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                        return;
                    }

                    if (result == null || !result.Valid)
                    {
                        if (!string.IsNullOrEmpty(result?.ReasonText))
                        {
                            base.ThrowErrorAndReset(ErrorMessage.FromLiteral(result.ReasonText));
                        }
                        else if (!string.IsNullOrEmpty(result?.ReasonCode))
                        {
                            base.ThrowErrorAndReset(ErrorMessage.FromLiteral(
                                (GameFacade.Strings.GetString("210", result.ReasonCode) ?? "Unknown Error")
                                .Replace("EA.com", AuthClient.BaseUrl.Substring(7).TrimEnd('/'))
                            ));
                        }
                        else
                        {
                            base.ThrowErrorAndReset(new Exception("Unknown error"));
                        }
                    }
                    else
                    {
                        this.AuthResult = result;
                        AsyncTransition("InitialConnect");
                    }
                    break;
                case "InitialConnect":
                    try
                    {
                        var connectResult = await Task.Run(() =>
                            CityClient.InitialConnectServlet(
                                new InitialConnectServletRequest
                                {
                                    Ticket = AuthResult.Ticket,
                                    Version = "Version 1.1097.1.0"
                                }));

                        if (connectResult.Status == InitialConnectServletResultType.Authorized)
                        {
                            var cdnurl = connectResult.UserAuthorized.FSOCDNUrl;
                            if (cdnurl != null)
                                ApiClient.CDNUrl = cdnurl;

                            if (RequireUpdate(connectResult.UserAuthorized) && !FSOEnvironment.SoftwareKeyboard)
                            {
                                AsyncTransition("UpdateRequired", connectResult.UserAuthorized);
                            }
                            else
                            {
                                AsyncTransition("AvatarData");
                            }
                        }
                        else if (connectResult.Status == InitialConnectServletResultType.Error)
                        {
                            base.ThrowErrorAndReset(ErrorMessage.FromLiteral(connectResult.Error.Code, connectResult.Error.Message));
                        }
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;

                case "AvatarData":
                    try
                    {
                        Avatars = await Task.Run(() => CityClient.AvatarDataServlet());
                        AsyncTransition("ShardStatus");
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;

                case "ShardStatus":
                    try
                    {
                        ((ClientShards)Shards).All = await Task.Run(() => CityClient.ShardStatus());
                        AsyncTransition("LoggedIn");
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;
                case "LoggedIn":
                    FSOFacade.Controller.ShowPersonSelection();
                    break;
            }
        }

        public bool RequireUpdate(UserAuthorized auth)
        {
            if (auth.FSOVersion == null) return false;

            var str = GlobalSettings.Default.ClientVersion;
            var authstr = auth.FSOBranch + "-" + auth.FSOVersion;

            return str != authstr;
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        public void Login(AuthRequest request)
        {
            this.AsyncProcessMessage(request);
        }

        public void Logout()
        {
            this.AsyncTransition("NotLoggedIn");
        }
    }
}
