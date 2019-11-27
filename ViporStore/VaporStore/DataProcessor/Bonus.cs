namespace VaporStore.DataProcessor
{
	using System;
    using System.Linq;
    using Data;
    using VaporStore.Data.Models;

    public static class Bonus
	{
		public static string UpdateEmail(VaporStoreDbContext context, string username, string newEmail)
		{
            User user = context.Users
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return $"User {username} not found";
            }

            if (context.Users.Any(u => u.Email == newEmail))
            {
                return $"Email {newEmail} is already taken";
            }

            user.Email = newEmail;
            context.Users.Update(user);
            context.SaveChanges();

            return $"Changed {username}'s email successfully";
		}
	}
}
