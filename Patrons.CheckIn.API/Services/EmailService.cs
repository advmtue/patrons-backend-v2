using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

using Patrons.CheckIn.API.Models.MongoDatabase;
using Patrons.CheckIn.API.Database;

namespace Patrons.CheckIn.API.Services
{
    /// <summary>
    /// Templating information for sending a marketing welcome email to new users.
    /// </summary>
    public class MarketingWelcomeEmail
    {
        /// <summary>
        /// User name.
        /// </summary>
        [JsonPropertyName("name")]
        public string User { get; set; }

        /// <summary>
        /// User email address.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// Unsubscribe link.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("unsubscribe_link")]
        public string UnsubscribeLink { get; set; }
    }

    public interface IEmailService
    {
        Task SendMarketWelcome(MarketingUser mUser);
    }

    /// <summary>
    /// Service to handle sending marketing emails using templates.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IAmazonSimpleEmailService _emailClient;
        private readonly IPatronsDatabase _database;

        public EmailService(
                ILogger<EmailService> logger,
                IPatronsDatabase database,
                IAmazonSimpleEmailService emailClient)
        {
            // Save refs
            _logger = logger;
            _database = database;
            _emailClient = emailClient;
        }

        /// <summary>
        /// Send a marketing welcome email to a user who has signed up to recieve marketing emails.
        /// </summary>
        /// <param name="mUser">Marketing user information</param>
        /// <returns>None.</returns>
        public async Task SendMarketWelcome(MarketingUser mUser)
        {
            // Ensure the mUser is not null.
            if (mUser == null || mUser.Name == null || mUser.Email == null) throw new ArgumentNullException();

            // Create information for a new marketing email
            var data = new MarketingWelcomeEmail
            {
                User = mUser.Name,
                Email = mUser.Email,
                UnsubscribeLink = string.Format("https://patrons.at/email/unsubscribe/{0}", await _database.CreateMarketingUserUnsubscribeLink(mUser))
            };

            // Convert data to a JSON string.
            string jsonData = JsonSerializer.Serialize<MarketingWelcomeEmail>(data);

            // Required template information:
            //  * name: user name
            //  * email: email address
            //  * unsubscribe_link: unsubscribe link
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

            // Send the email.
            await _emailClient.SendTemplatedEmailAsync(emailRequest);
        }
    }
}
