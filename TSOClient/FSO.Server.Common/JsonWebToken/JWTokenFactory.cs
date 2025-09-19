using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTInstance
    {
        public string Token;
        public int ExpiresIn;
    }

    public class JWTFactory
    {
        private JWTConfiguration Config;

        public JWTFactory(JWTConfiguration config)
        {
            this.Config = config;
        }

        public JWTUser DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Config.Key;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null)
                    return null;

                var dataClaim = jwtToken.Payload["data"]?.ToString();
                if (dataClaim == null)
                    return null;

                return JsonConvert.DeserializeObject<JWTUser>(dataClaim);
            }
            catch
            {
                return null; // token invalid or expired
            }
        }

        public JWTInstance CreateToken(JWTUser data)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Config.Key;

            var tokenData = JsonConvert.SerializeObject(data);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddSeconds(Config.TokenDuration),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha384Signature),
                Claims = new Dictionary<string, object>
                {
                    { "data", tokenData }
                }
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new JWTInstance
            {
                Token = tokenString,
                ExpiresIn = Config.TokenDuration
            };
        }
    }
}
