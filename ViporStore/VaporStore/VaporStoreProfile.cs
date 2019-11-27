namespace VaporStore
{
	using AutoMapper;
    using System;
    using VaporStore.Data.Models;
    using VaporStore.Data.Models.Enums;
    using VaporStore.DataProcessor.ImportDtos;

    public class VaporStoreProfile : Profile
	{
		// Configure your AutoMapper here if you wish to use it. If not, DO NOT DELETE THIS CLASS
		public VaporStoreProfile()
		{
            this.CreateMap<CardImportDto, Card>()
                .ForMember(x => x.Type, y => y.MapFrom(x => Enum.Parse(typeof(CardType), x.Type)));

            this.CreateMap<UserImportDto, User>();
		}
	}
}