using ApplicationCore.Models;

namespace ApplicationCore.Interfaces
{
    public interface IAccountService
    {
        Task SignIn(AccountModel model);
        Task SignUp(AccountModel model);
    }
}
