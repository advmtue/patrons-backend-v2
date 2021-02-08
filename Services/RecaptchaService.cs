using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace patrons_web_api.Services
{
    /// <summary>
    /// Configuration settings for recaptcha
    /// </summary>
    public class RecaptchaV3Settings : IRecaptchaV3Settings
    {
        /// <summary>
        /// Secret key as distributed by Google Console.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Required confidence threshold for a request to pass.
        /// </summary>
        /// <value></value>
        public double ConfidenceThreshold { get; set; }
    }

    /// <summary>
    /// Interface for RecaptchaV3Settings.
    /// </summary>
    public interface IRecaptchaV3Settings
    {
        string SecretKey { get; set; }
        double ConfidenceThreshold { get; set; }
    }

    public interface IRecaptchaService
    {
        Task<bool> Validate(string token);
    }

    /// <summary>
    /// Response object from Google Recaptcha servers after making a HTTP request.
    /// </summary>
    public class RecaptchaResponse
    {
        /// <summary>
        /// Overall success status.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Confidence score.
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; }

        /// <summary>
        /// Name of the action which was supplied in the request.
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; }

        /// <summary>
        /// Challenge timestamp.
        /// </summary>
        [JsonPropertyName("challenge_ts")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Hostname of the request.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }
    }

    public class RecaptchaService : IRecaptchaService
    {
        private IRecaptchaV3Settings _settings;
        private readonly ILogger<RecaptchaService> _logger;
        private readonly HttpClient _client = new HttpClient();

        public RecaptchaService(IRecaptchaV3Settings settings, ILogger<RecaptchaService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Validate a response token against the Google recaptcha API.
        /// Upon validation, compare response score against required confidence threshold as defined in settings.
        /// </summary>
        /// <param name="token">Client response token</param>
        /// <returns>True if the confidence score passed the threshold.</returns>
        public async Task<bool> Validate(string token)
        {
            // Create a new recaptcha verification request payload.
            Dictionary<string, string> request = new Dictionary<string, string>()
            {
                { "secret", _settings.SecretKey},
                { "response", token }
            };

            // Turn the request payload string into a HttpContent instance.
            var content = new FormUrlEncodedContent(request);

            // Perform the request.
            var clientResponse = await _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

            // Convert the response information into a RecaptchaResponse for processing.
            var responseOb = JsonSerializer.Deserialize<RecaptchaResponse>(await clientResponse.Content.ReadAsStringAsync());

            // Determine if the confidence interval is met.
            return responseOb.Score > _settings.ConfidenceThreshold;
        }
    }
}