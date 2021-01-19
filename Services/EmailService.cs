using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.Json;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class MarketingWelcomeEmail
    {
        [JsonPropertyName("name")]
        public string User { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("unsubscribe_link")]
        public string UnsubscribeLink { get; set; }
    }

    public interface IEmailService
    {
        Task SendMarketWelcome(MarketingUser mUser);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly AmazonSimpleEmailServiceClient _emailClient;
        private readonly IPatronsDatabase _database;

        public EmailService(ILogger<EmailService> logger, IPatronsDatabase database)
        {
            _logger = logger;
            _database = database;

            // Create a new email client
            _emailClient = new AmazonSimpleEmailServiceClient(RegionEndpoint.APSoutheast2);
        }

        public async Task SendMarketWelcome(MarketingUser mUser)
        {
            var data = new MarketingWelcomeEmail
            {
                User = mUser.Name,
                Email = mUser.Email,
                UnsubscribeLink = string.Format("https://patrons.at/email/unsubscribe/{0}", await _database.CreateMarketingUserUnsubscribeLink(mUser))
            };

            // Convert data to a JSON string
            string jsonData = JsonSerializer.Serialize<MarketingWelcomeEmail>(data);

            // Required template information:
            // name :: user name
            // email :: email address
            // unsubscribe_link :: unsubscribe link
            var emailRequest = new SendTemplatedEmailRequest()
            {
                Source = "info@patrons.at",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { mUser.Email }
                },
                Template = "marketing-welcome",
                TemplateData = jsonData,
            };

            await _emailClient.SendTemplatedEmailAsync(emailRequest);
        }
    }
}