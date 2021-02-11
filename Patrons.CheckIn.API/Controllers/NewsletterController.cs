using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Patrons.CheckIn.API.Models.MongoDatabase;
using Patrons.CheckIn.API.Models.Transfer.Response;
using Patrons.CheckIn.API.Services;

/// <summary>
/// Controller for handling requests when prospecting users sign up to recieve newsletter information.
/// </summary>
namespace Patrons.CheckIn.API.Controllers
{
    /// <summary>
    /// Newsletter registration request.
    /// </summary>
    public class NewsLetterRegistration
    {
        /// <summary>
        /// User name. Can be first name, or first name and last name.
        /// </summary>
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// User email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        // RecaptchaV3 token.
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

        /// <summary>
        /// Anonymous user attempts to register for patron's newsletter.
        /// Implements recaptcha to filter out bot requests.
        /// </summary>
        /// <param name="registration">Newletter registration information.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> RegisterForNewsletter(NewsLetterRegistration registration)
        {
            // Attempt to validate the captcha code as an anti-spam mechanism.
            bool captchaPassedValidation;
            try
            {
                captchaPassedValidation = await _captcha.Validate(registration.Captcha);
            }
            catch (Exception ex)
            {
                // Error: Unknown error.
                _logger.LogError(ex, "An unknown exception occurred");

                return BadRequest(APIError.UnknownError());
            }

            // If the captcha failed validation, return bad request.
            if (!captchaPassedValidation)
            {
                return BadRequest(APIError.RecaptchaFailure());
            }

            MarketingUser marketingUser;
            try
            {
                // Register the user's email address in the database.
                marketingUser = await _newsletter.RegisterUser(registration.Name, registration.Email);
            }
            catch (MarketingUserAlreadySubscribedException ex)
            {
                // Error: User is already subscribed in the database.
                _logger.LogWarning(ex, "Failed to register marketing user. Email is already registered: {}", registration.Email);

                return BadRequest(APIError.MarketingUserAlreadySubscribed());
            }
            catch (Exception ex)
            {
                // Error: Unknown error.
                _logger.LogError(ex, "Exception occurred when attempting to register a marketing user.");

                return BadRequest(APIError.UnknownError());
            }

            try
            {
                // Send confirmation email to the user with unsubscribe link
                await _email.SendMarketWelcome(marketingUser);
            }
            catch (Exception ex)
            {
                // Error: Unknown error.
                _logger.LogError(ex, "An unknown error occurred when attempting to send marketing-welcome email.");

                return BadRequest(APIError.UnknownError());
            }

            return Ok();
        }

        /// <summary>
        /// Anonymous user attempts to unsubscribe an email by using an unsubscription ID.
        /// </summary>
        /// <param name="unsubscribeId">Unsubscribe ID.</param>
        /// <returns>Empty OK status on success, or an error.</returns>
        [AllowAnonymous]
        [HttpDelete("unsubscribe/{unsubscribeId}")]
        public async Task<IActionResult> UnsubscribeFromMarketing([FromRoute] string unsubscribeId)
        {
            try
            {
                // Unsubscribe the user from marketing emails.
                await _newsletter.UnsubscribeFromMarketing(unsubscribeId);

                // Return empty OK status.
                return Ok();
            }
            catch (Exception ex)
            {
                // Error: Unknown error.
                _logger.LogWarning(ex, "Failed to unsubscribe user from marketing emails due to unknown error.");

                return BadRequest(APIError.UnknownError());
            }
        }
    }
}
