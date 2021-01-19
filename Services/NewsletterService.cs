using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using patrons_web_api.Database;
using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Services
{
    public class MarketingUserAlreadySubscribedException : Exception { }
    public interface INewsletterService
    {
        Task<MarketingUser> RegisterUser(string name, string email);
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
        /// Register a user, email combination for recieving marketing emails. Intelligently
        /// select whether to create new users or update existing user information.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <returns></returns>
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

            return mu;
        }
    }
}