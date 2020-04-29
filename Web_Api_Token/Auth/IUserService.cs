using Web_Api_Token.Models;

namespace Web_Token.Auth
{
    public interface IUserService
    {
        bool IsValid(LoginRequestDTO req);
    }
}
