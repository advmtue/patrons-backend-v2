using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Patrons.CheckIn.API.Services
{
    public class RecaptchaNullContentException : Exception {};

    /// <summary>
    /// Configuration settings for recaptcha validation.
    /// </summary>
    public class RecaptchaValidationSettings : IRecaptchaValidationSettings
    {
        /// <summary>
        /// Required confidence threshold for a request to pass.
        /// </summary>
        /// <value></value>
        public double ConfidenceThreshold { get; set; }
    }

    /// <summary>
    /// Interface for RecaptchaV3Settings.
    /// </summary>
    public interface IRecaptchaValidationSettings
    {
        double ConfidenceThreshold { get; set; }
    }

    public interface IRecaptchaValidationService
    {
        Task<bool> Validate(string token);
    }


    public class RecaptchaValidationService : IRecaptchaValidationService
    {
        private IRecaptchaValidationSettings _settings;
        private readonly ILogger<RecaptchaValidationService> _logger;
        private readonly IRecaptchaWebService _recaptcha;

        public RecaptchaValidationService(
                IRecaptchaValidationSettings settings,
                ILogger<RecaptchaValidationService> logger,
                IRecaptchaWebService recaptcha)
        {
            _settings = settings;
            _logger = logger;
            _recaptcha = recaptcha;
        }

        /// <summary>
        /// Validate a response token against the Google recaptcha API.
        /// Upon validation, compare response score against required confidence threshold as defined in settings.
        /// </summary>
        /// <param name="token">Client response token</param>
        /// <returns>True if the confidence score passed the threshold.</returns>
        public async Task<bool> Validate(string token)
        {
            // Throw an exception if the token is null.
            if (token == null) throw new ArgumentNullException();

            // Get a response from google console via the recaptcha web service.
            var response = await _recaptcha.Get(token);

            // Determine if the confidence interval is met.
            return response.Score >= _settings.ConfidenceThreshold;
        }
    }
}
