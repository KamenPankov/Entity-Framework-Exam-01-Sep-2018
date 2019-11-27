namespace VaporStore.DataProcessor
{
	using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Newtonsoft.Json;
    using VaporStore.Data.Models.Enums;
    using VaporStore.DataProcessor.ExportDtos;

    public static class Serializer
	{
		public static string ExportGamesByGenres(VaporStoreDbContext context, string[] genreNames)
		{
            var gamesByGeners = context.Genres
                .Where(g => genreNames.Contains(g.Name))
                .Select(g => new
                {
                    Id = g.Id,
                    Genre = g.Name,
                    Games = g.Games
                    .Where(gm => gm.Purchases.Any())
                    .Select(gm => new
                    {
                        Id = gm.Id,
                        Title = gm.Name,
                        Developer = gm.Developer.Name,
                        Tags = string.Join(", ", gm.GameTags.Select(t => t.Tag.Name)),
                        Players = gm.Purchases.Count()
                    })
                    .OrderByDescending(gm => gm.Players)
                    .ThenBy(gm => gm.Id)
                    .ToArray(),
                    TotalPlayers = g.Games.Sum(gm => gm.Purchases.Count())
                })
                .OrderByDescending(g => g.TotalPlayers)
                .ThenBy(g => g.Id)
                .ToArray();

            string jsonString = JsonConvert.SerializeObject(gamesByGeners);//, Formatting.Indented);

            return jsonString;
		}

		public static string ExportUserPurchasesByType(VaporStoreDbContext context, string storeType)
		{
            UserExportDto[] userPurchasesByType = context.Users                
                .Select(u => new UserExportDto()
                {
                    Username = u.Username,
                    Purchases = u.Cards
                    .SelectMany(c => c.Purchases)
                    .Where(p => p.Type == Enum.Parse<PurchaseType>(storeType))
                    .OrderBy(p => p.Date)
                    .Select(p => new PurchaseExportDto()
                    {
                        CardNumber = p.Card.Number,
                        CardCvc = p.Card.Cvc,
                        Date = p.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        Game = new GameExportDto()
                        {
                            GameName = p.Game.Name,
                            Genre = p.Game.Genre.ToString(),
                            Price = p.Game.Price
                        }
                    })
                    //.OrderBy(p => p.Date)
                    .ToArray(),
                    TotalSpent = u.Cards.SelectMany(p => p.Purchases)
                    .Where(p => p.Type == Enum.Parse<PurchaseType>(storeType))
                    .Sum(g => g.Game.Price)
                })
                .Where(u => u.Purchases.Any())
                .OrderByDescending(u => u.TotalSpent)
                .ThenBy(u => u.Username)
                .ToArray();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserExportDto[]), new XmlRootAttribute("Users"));

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            StringBuilder stringBuilder = new StringBuilder();

            using (StringWriter stringWriter = new StringWriter(stringBuilder))
            {
                xmlSerializer.Serialize(stringWriter, userPurchasesByType, namespaces);
            }

            return stringBuilder.ToString().TrimEnd();
        }
	}
}