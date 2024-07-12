using ApiTemplate.Application.Entities;
using ApiTemplate.Application.Models;
using ApiTemplate.Application.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Tests.Account
{
    public class AccountTests : BaseIntegrationTest
    {
        public AccountTests(CustomWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpResponseMessage> SignUp(AccountSignInDto model)
         => await HTTPClient.PostAsJsonAsync("api/account/signUp", model);

        private async Task<HttpResponseMessage> SignIn(AccountSignInDto model)
         => await HTTPClient.PostAsJsonAsync("api/account/signIn", model);

        private async Task<HttpResponseMessage> SendDigitCode(AccountSignInDto model)
         => await HTTPClient.PostAsJsonAsync("api/account/digitCode", model.Email);

        private async Task<HttpResponseMessage> ConfirmUserEmail(string code)
         => await HTTPClient.PutAsJsonAsync("api/account/digitCode", code);

        private async Task UpdateUserEmailConfirm(AccountSignInDto model, bool isConfirm)
        {
            var userRepo = ServicesScope.ServiceProvider.GetRequiredService<IRepo<ApplicationUserEntity>>();
            var user = await userRepo.GetIQueryable().FirstOrDefaultAsync(x => x.Email == model.Email);
            if (user is null) return;
            user!.EmailConfirmed = isConfirm;
            await userRepo.UpdateAsync(user, true, CancellationToken.None, c => c.EmailConfirmed);
        }

        [Fact]
        public async Task Should_Delete_Same_Unconfirmed_User()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestUp",
                FirstName = "TestUp",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaUp05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            var first = await SignUp(userModel);
            var second = await SignUp(userModel);
            // Assert
            Assert.True(first.IsSuccessStatusCode && second.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_SignOut_User()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestSignOut",
                FirstName = "TestSignOut",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSignOut05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            await SignUp(userModel);
            await UpdateUserEmailConfirm(userModel, true);
            await SignIn(userModel);
            var signOutRes1 = await HTTPClient.PostAsync("api/account/signOut", null);
            var signOutRes2 = await HTTPClient.PostAsync("api/account/signOut", null);
            // Assert
            Assert.True(signOutRes1.IsSuccessStatusCode && !signOutRes2.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Not_Reach_Secure_Endpoint_Besides_SuperAdmin()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestSuper",
                FirstName = "TestSuper",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSuper05@gmail.com",
                Role = RoleEnum.Admin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            var signUpR = await SignUp(userModel);
            await UpdateUserEmailConfirm(userModel, true);
            var signInR = await SignIn(userModel);
            var protectedByAuth = await HTTPClient.PostAsync("api/account/onlyForSupAdmin", null);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && !protectedByAuth.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Reach_Secure_Endpoint_By_SuperAdmin()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestSuper",
                FirstName = "TestSuper",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSuper05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            var signUpR = await SignUp(userModel);
            await UpdateUserEmailConfirm(userModel, true);
            var signInR = await SignIn(userModel);
            var protectedByAuth = await HTTPClient.PostAsync("api/account/onlyForSupAdmin", null);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && protectedByAuth.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Confrim_User_Email()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestDigitCode",
                FirstName = "TestDigitCode",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaDigitCode05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            var userTokenRepo = ServicesScope.ServiceProvider.GetRequiredService<IRepo<IdentityUserTokenEntity>>();
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            var signUpR = await SignUp(userModel);
            var sendRes = await SendDigitCode(userModel);

            var isNewTokenAppears = await userTokenRepo.GetIQueryable().Where(x => x.User.Email == userModel.Email).FirstOrDefaultAsync();
            var confirmRes = await ConfirmUserEmail(isNewTokenAppears!.Name);
            // Assert
            Assert.True(sendRes.IsSuccessStatusCode && confirmRes.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Delete_User()
        {
            // Arrange
            var userModel = new AccountSignInDto()
            {
                RememberMe = true,
                LastName = "TestDelete",
                FirstName = "TestDelete",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitadelete05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel, false);
            var signUpR = await SignUp(userModel);
            await UpdateUserEmailConfirm(userModel, true);
            var signInR = await SignIn(userModel);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "api/account");
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(userModel.Password), Encoding.UTF8, "application/json");
            var deleteR = await HTTPClient.SendAsync(requestMessage);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && deleteR.IsSuccessStatusCode);
        }
    }
}