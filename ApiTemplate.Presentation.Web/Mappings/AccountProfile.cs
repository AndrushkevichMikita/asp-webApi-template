using ApiTemplate.Application.Models;
using ApiTemplate.Presentation.Web.Models;
using AutoMapper;

namespace ApiTemplate.Presentation.Web.Mappings
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Source => Target
            CreateMap<CreateAccountModel, CreateAccountDto>();
            CreateMap<LoginAccountModel, LoginAccountDto>();
            CreateMap<RefreshTokenModel, RefreshTokenDto>();
            CreateMap<RefreshTokenDto, RefreshTokenModel>();
            CreateMap<AccountDto, AccountModel>();
        }
    }
}
