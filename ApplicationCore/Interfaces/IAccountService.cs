using ApplicationCore.Models;

namespace ApplicationCore.Interfaces
{
    public interface IAccountService
    {
        Task SignOut();
        Task SignIn(AccountSignInDto model);
        Task SignUp(AccountSignInDto model);
        Task ConfirmDigitCode(string digitCode);
        Task SendDigitCodeByEmail(string email);
        Task Delete(string password, int accountId);
        Task<AccountBaseDto> GetCurrent(int userId);
    }
}
