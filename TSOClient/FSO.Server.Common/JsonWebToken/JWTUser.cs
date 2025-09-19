namespace FSO.Server.Servers.Api.JsonWebToken
{
    public interface IUserIdentity
    {
        string UserName { get; }
        IEnumerable<string> Claims { get; }
    }


    public class JWTUser
    {
        public uint UserID { get; set; }
        public string UserName { get; set; }

        public List<string> Claims { get; set; } = new List<string>();
    }

    public class JWTUserIdentity : JWTUser, IUserIdentity
    {
        IEnumerable<string> IUserIdentity.Claims => Claims;
    }
}
