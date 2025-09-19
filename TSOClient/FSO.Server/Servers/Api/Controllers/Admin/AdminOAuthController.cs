using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Servers.Api.JsonWebToken;
using Nancy;

namespace FSO.Server.Servers.Api.Controllers.Admin
{
    public class AdminOAuthController : NancyModule
    {
        public AdminOAuthController(IDAFactory daFactory, JWTFactory jwt) : base("/admin/oauth")
        {
            Post("/token", _ =>
            {
                string grant_type = (string)this.Request.Form.grant_type;

                if (grant_type == "password")
                {
                    string username = (string)this.Request.Form.username;
                    string password = (string)this.Request.Form.password;

                    using (var da = daFactory.Get())
                    {
                        var user = da.Users.GetByUsername(username);
                        if (user == null || user.is_banned || !(user.is_admin || user.is_moderator))
                        {
                            return Response.AsJson(new OAuthError
                            {
                                error = "unauthorized_client",
                                error_description = "user_credentials_invalid"
                            });
                        }

                        var authSettings = da.Users.GetAuthenticationSettings(user.user_id);
                        var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                        {
                            data = authSettings.data,
                            scheme = authSettings.scheme_class
                        });

                        if (!isPasswordCorrect)
                        {
                            return Response.AsJson(new OAuthError
                            {
                                error = "unauthorized_client",
                                error_description = "user_credentials_invalid"
                            });
                        }

                        // Initialize Claims as a mutable list
                        JWTUserIdentity identity = new JWTUserIdentity
                        {
                            UserName = user.username,
                            UserID = user.user_id,
                            Claims = new List<string>()
                        };

                        if (user.is_admin || user.is_moderator)
                            identity.Claims.Add("moderator");
                        if (user.is_admin)
                            identity.Claims.Add("admin");

                        var token = jwt.CreateToken(identity);
                        return Response.AsJson(new OAuthSuccess
                        {
                            access_token = token.Token,
                            expires_in = token.ExpiresIn
                        });
                    }
                }

                return Response.AsJson(new OAuthError
                {
                    error = "invalid_request",
                    error_description = "unknown grant_type"
                });
            });
        }
    }

    public class OAuthError
    {
        public string error_description { get; set; }
        public string error { get; set; }
    }

    public class OAuthSuccess
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }
}