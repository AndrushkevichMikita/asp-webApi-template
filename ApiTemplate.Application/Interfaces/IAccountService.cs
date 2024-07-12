using ApiTemplate.Application.Models;

namespace ApiTemplate.Application.Interfaces
{
    public interface IAccountService
    {
        Task SignOut();
        Task<(string token, string refreshToken)> SignIn(AccountSignInDto model);
        Task SignUp(AccountSignInDto model);
        Task ConfirmDigitCode(string digitCode);
        Task SendDigitCodeByEmail(string email);
        Task Delete(string password, int accountId);
        Task<AccountBaseDto> GetCurrent(int userId);
    }
}
