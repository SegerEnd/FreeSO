namespace FSO.Server.Servers.Api.JsonWebToken
{
    public interface IUserIdentity
    {
        string UserName { get; }
        IEnumerable<string> Claims { get; }
    }

    public class JWTUserIdentity : JWTUser, IUserIdentity
    {
        // Explicit interface implementation
        IEnumerable<string> IUserIdentity.Claims => _claims;
    }
}
