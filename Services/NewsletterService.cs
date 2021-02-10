using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Models.MongoDatabase;

namespace Patrons.CheckIn.API.Services
{
    public class MarketingUserAlreadySubscribedException : Exception { }
    public interface INewsletterService
    {
        Task<MarketingUser> RegisterUser(string name, string email);
        Task UnsubscribeFromMarketing(string unsubscribeId);
    }

    public class NewsletterService : INewsletterService
    {
        private readonly ILogger<NewsletterService> _logger;
        private readonly IPatronsDatabase _database;

        public NewsletterService(ILogger<NewsletterService> logger, IPatronsDatabase database)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Register a user + email combination for recieving marketing emails. Intelligently
        /// select whether to create new users or update existing user information.
        /// </summary>
        /// <param name="name">User's name</param>
        /// <param name="email">User's email address</param>
        /// <returns>Marketing user as saved in the database.</returns>
        public async Task<MarketingUser> RegisterUser(string name, string email)
        {
            MarketingUser mu;
            try
            {
                // Check if there is a matching email that is subscribed
                mu = await _database.GetActiveMarketingUserByEmail(email);

                // User exists and is subscribed
                throw new MarketingUserAlreadySubscribedException();
            }
            catch (MarketingUserNotFoundException)
            {
                // If not, add to database and mark for recieving marketing emails
                mu = await _database.CreateMarketingUser(name, email);
            }

            // Return the new marketing user.
            return mu;
        }

        /// <summary>
        /// Unsubscribe a user from marketing emails using an unsubscribe link.
        /// </summary>
        /// <param name="unsubscribeId">Unsubscribe link ID</param>
        /// <returns></returns>
        public async Task UnsubscribeFromMarketing(string unsubscribeId)
        {
            // Unsubscribe the user.
            await _database.UnsubscribeFromMarketing(unsubscribeId);
        }
    }
}
