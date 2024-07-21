using ApiTemplate.Application.Models;
using ApiTemplate.Domain.Entities;
using AutoMapper;

namespace CommandsService.Profiles
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Source => Target
            CreateMap<AccountEntity, AccountDto>();
            CreateMap<AccountEntity, AccountDto>();
            CreateMap<AccountDto, AccountEntity>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
        }
    }
}