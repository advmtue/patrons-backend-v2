using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Models.Transfer.Response;
using patrons_web_api.Services;

namespace patrons_web_api.Controllers
{
    public class NewsLetterRegistration
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [Required]
        [JsonPropertyName("captcha")]
        public string Captcha { get; set; }
    }

    [Route("news")]
    [ApiController]
    public class NewsletterController : ControllerBase
    {
        private readonly ILogger<NewsletterController> _logger;
        private IRecaptchaService _captcha;
        private INewsletterService _newsletter;
        private IEmailService _email;

        public NewsletterController(
            ILogger<NewsletterController> logger,
            IRecaptchaService captcha,
            INewsletterService newsletter,
            IEmailService email)
        {
            _logger = logger;
            _captcha = captcha;
            _newsletter = newsletter;
            _email = email;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> RegisterForNewsletter(NewsLetterRegistration registration)
        {
            // Attempt to validate the captcha code as an anti-spam mechanism
            bool captchaPassedValidation;
            try
            {
                captchaPassedValidation = await _captcha.Validate(registration.Captcha);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unknown exception occurred");

                return BadRequest();
            }

            // If the captcha failed validation, return bad request
            if (!captchaPassedValidation)
            {
                return BadRequest();
            }

            // Register the email in the database
            MarketingUser marketingUser;
            try
            {
                marketingUser = await _newsletter.RegisterUser(registration.Name, registration.Email);
            }
            catch (MarketingUserAlreadySubscribedException ex)
            {
                _logger.LogWarning(ex, "Failed to register marketing user. Email is already registered: {}", registration.Email);

                return BadRequest(APIError.MarketingUserAlreadySubscribed());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred when attempting to register a marketing user.");
                return BadRequest();
            }


            // Send confirmation email to the user with unsubscribe link
            try
            {
                await _email.SendMarketWelcome(marketingUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to user");

                return BadRequest();
            }

            // Send update email to sales@patrons.at (feeback loop boy)

            // Finish request
            return await Task.FromResult(Ok());
        }
    }
}