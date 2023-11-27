using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Identity;

namespace ApplicationCore.Services
{
    public class AccountService : IAccountService
    {
        private readonly SignInManager<UserEntity> _signManager;
        private readonly UserManager<UserEntity> _manager;

        public AccountService(SignInManager<UserEntity> signManager,
                              UserManager<UserEntity> manager)
        {
            _signManager = signManager;
            _manager = manager;
        }

        private async Task<UserEntity?> DeleteSameNotConfirmed(string email)
        {
            var existingUser = await _manager.FindByEmailAsync(email);
            if (existingUser == null) return null;

            if (existingUser.EmailConfirmed)
                throw new MyApplicationException(ErrorStatus.InvalidData, "User already exists");

            await _manager.DeleteAsync(existingUser);
            return existingUser;
        }

        public async Task SignIn(AccountModel model)
        {
            var appUser = await _manager.FindByEmailAsync(model.Email) ?? throw new MyApplicationException(ErrorStatus.NotFound, "User not found");
            var res = await _signManager.PasswordSignInAsync(appUser, model.Password, false, false);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, "Password or user invalid");
        }

        public async Task SignUp(AccountModel model)
        {
            await DeleteSameNotConfirmed(model.Email);

            var toInsert = new UserEntity
            {
                Role = model.Role,
                Email = model.Email,
                EmailConfirmed = true,
                UserName = model.UserName ?? model.Email,
            };

            var res = await _manager.CreateAsync(toInsert, model.Password);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, string.Join(" ", res.Errors.Select(c => c.Description)));
            await _manager.AddToRoleAsync(toInsert, toInsert.Role.ToString());
        }
    }
}
