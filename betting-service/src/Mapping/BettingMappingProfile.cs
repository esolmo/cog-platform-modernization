using AutoMapper;
using BettingService.Models.Responses;
using Cog.Domain.Entities;

namespace BettingService.Mapping;

public class BettingMappingProfile : Profile
{
    public BettingMappingProfile()
    {
        CreateMap<Wager, WagerResponse>()
            .ForMember(d => d.CustomerLoginName, o => o.MapFrom(s => s.Customer != null ? s.Customer.LoginName : string.Empty));

        CreateMap<WagerItem, WagerItemResponse>()
            .ForMember(d => d.HomeTeam, o => o.MapFrom(s => s.GamePeriod != null && s.GamePeriod.Game != null ? s.GamePeriod.Game.HomeTeam : string.Empty))
            .ForMember(d => d.AwayTeam, o => o.MapFrom(s => s.GamePeriod != null && s.GamePeriod.Game != null ? s.GamePeriod.Game.AwayTeam : string.Empty))
            .ForMember(d => d.PeriodDescription, o => o.MapFrom(s => s.GamePeriod != null ? s.GamePeriod.PeriodDescription : string.Empty));

        CreateMap<Game, GameResponse>()
            .ForMember(d => d.SportName, o => o.MapFrom(s => s.SportType != null ? s.SportType.Name : string.Empty));

        CreateMap<GamePeriod, GamePeriodResponse>()
            .ForMember(d => d.Lines, o => o.MapFrom(s => s.LineSet));
        CreateMap<LineSet, LineSetResponse>();
        CreateMap<SportType, SportTypeResponse>();
    }
}
