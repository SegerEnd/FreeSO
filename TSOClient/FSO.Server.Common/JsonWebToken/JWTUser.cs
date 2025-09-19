namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTUser
    {
        public uint UserID { get; set; }

        protected List<string> _claims = new List<string>();

        public List<string> ClaimsInternal => _claims;

        public string UserName { get; set; }
    }
}
