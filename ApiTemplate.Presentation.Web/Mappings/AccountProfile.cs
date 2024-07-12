using ApiTemplate.Application.Entities;
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
            CreateMap<CreateAccountModel, AccountDto>();
            CreateMap<LoginAccountModel, AccountDto>();
            CreateMap<RefreshTokenModel, RefreshTokenDto>();
            CreateMap<AccountDto, AccountModel>();
        }
    }
}
