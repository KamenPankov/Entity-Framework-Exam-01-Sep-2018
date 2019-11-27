namespace VaporStore.DataProcessor
{
	using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using AutoMapper;
    using Data;
    using Newtonsoft.Json;
    using VaporStore.Data.Models;
    using VaporStore.Data.Models.Enums;
    using VaporStore.DataProcessor.ImportDtos;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    public static class Deserializer
	{
		public static string ImportGames(VaporStoreDbContext context, string jsonString)
		{
            GameImportDto[] gameImportDtos = JsonConvert.DeserializeObject<GameImportDto[]>(jsonString);

            StringBuilder stringBuilder = new StringBuilder();

            List<Game> games = new List<Game>();

            foreach (GameImportDto gameImportDto in gameImportDtos)
            {
                if (!IsValid(gameImportDto))
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                if (!DateTime.TryParseExact(gameImportDto.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                if (gameImportDto.Tags.Length == 0)
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                Developer developerExists = context.Developers.FirstOrDefault(d => d.Name == gameImportDto.Developer);
                if (developerExists == null)
                {
                    developerExists = new Developer() { Name = gameImportDto.Developer };
                    context.Developers.Add(developerExists);
                    context.SaveChanges();
                }                

                Genre genreExists = context.Genres.FirstOrDefault(g => g.Name == gameImportDto.Genre);
                if (genreExists == null)
                {
                    genreExists = new Genre() { Name = gameImportDto.Genre };
                    context.Genres.Add(genreExists);
                    context.SaveChanges();
                }                

                Game game = new Game()
                {
                    Name = gameImportDto.Name,
                    Price = gameImportDto.Price,
                    ReleaseDate = DateTime.ParseExact(gameImportDto.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Developer = developerExists,
                    Genre = genreExists
                };

                List<GameTag> gameTags = new List<GameTag>();

                foreach (string tag in gameImportDto.Tags)
                {
                    Tag tagExists = context.Tags.FirstOrDefault(t => t.Name == tag);
                    if (tagExists == null)
                    {
                        tagExists = new Tag() { Name = tag };
                        context.Tags.Add(tagExists);
                        context.SaveChanges();
                    }                    

                    GameTag gameTag = new GameTag()
                    {
                        Game = game,
                        Tag = tagExists
                    };

                    gameTags.Add(gameTag);                    
                }

                game.GameTags = gameTags;

                games.Add(game);

                stringBuilder.AppendLine($"Added {game.Name} ({game.Genre.Name}) with {game.GameTags.Count()} tags");
            }

            context.Games.AddRange(games);

            context.SaveChanges();
            //Console.WriteLine(stringBuilder.ToString().TrimEnd());

            return stringBuilder.ToString().TrimEnd();

        }

		public static string ImportUsers(VaporStoreDbContext context, string jsonString)
		{
            UserImportDto[] userImportDtos = JsonConvert.DeserializeObject<UserImportDto[]>(jsonString);

            StringBuilder stringBuilder = new StringBuilder();

            List<User> users = new List<User>();

            foreach (UserImportDto userImportDto in userImportDtos)
            {
                if (!IsValid(userImportDto))
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                if (userImportDto.Cards.Length == 0)
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                if (userImportDto.Cards.Any(c => !IsValid(c)))
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                User user = Mapper.Map<User>(userImportDto);

                users.Add(user);

                stringBuilder.AppendLine($"Imported {user.Username} with {user.Cards.Count()} cards");
            }

            context.Users.AddRange(users);

            context.SaveChanges();
            //Console.WriteLine(stringBuilder.ToString().TrimEnd());

            return stringBuilder.ToString().TrimEnd();
        }

		public static string ImportPurchases(VaporStoreDbContext context, string xmlString)
		{
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PurchaseImportDto[]), new XmlRootAttribute("Purchases"));

            PurchaseImportDto[] purchaseImportDtos;

            using (StringReader stringReader = new StringReader(xmlString))
            {
                purchaseImportDtos = (PurchaseImportDto[])xmlSerializer.Deserialize(stringReader);
            }

            StringBuilder stringBuilder = new StringBuilder();

            List<Purchase> purchases = new List<Purchase>();

            foreach (PurchaseImportDto purchaseImportDto in purchaseImportDtos)
            {
                Card card = context.Cards.FirstOrDefault(c => c.Number == purchaseImportDto.CardNumber);
                if (card == null)
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                Game game = context.Games.FirstOrDefault(g => g.Name == purchaseImportDto.GameName);
                if (game == null)
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                Purchase purchase = new Purchase()
                {
                    Type = Enum.Parse<PurchaseType>(purchaseImportDto.Type),
                    ProductKey = purchaseImportDto.ProductKey,
                    Date = DateTime.ParseExact(purchaseImportDto.Date, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                    Card = card,
                    Game = game
                };

                if (!IsValid(purchase))
                {
                    stringBuilder.AppendLine("Invalid Data");
                    continue;
                }

                purchases.Add(purchase);

                stringBuilder.AppendLine($"Imported {purchase.Game.Name} for {purchase.Card.User.Username}");
            }

            context.Purchases.AddRange(purchases);

            context.SaveChanges();
            //Console.WriteLine(stringBuilder.ToString().TrimEnd());

            return stringBuilder.ToString().TrimEnd();
        }

        private static bool IsValid(object entity)
        {
            ValidationContext validationContext = new ValidationContext(entity);
            List<ValidationResult> validationResult = new List<ValidationResult>();

            var result = Validator.TryValidateObject(entity, validationContext, validationResult, true);

            return result;
        }
	}
}