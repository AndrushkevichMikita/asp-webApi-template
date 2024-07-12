using ApiTemplate.Application.Entities;
using ApiTemplate.Application.Models;
using AutoMapper;

namespace CommandsService.Profiles
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Source => Target
            CreateMap<ApplicationUserEntity, AccountDto>();
            CreateMap<AccountDto, ApplicationUserEntity>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
        }
    }
}
