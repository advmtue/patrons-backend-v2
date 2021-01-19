using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace patrons_web_api.Services
{
    public class RecaptchaV3Settings : IRecaptchaV3Settings
    {
        public string SecretKey { get; set; }
        public double ConfidenceThreshold { get; set; }
    }

    public interface IRecaptchaV3Settings
    {
        string SecretKey { get; set; }
        double ConfidenceThreshold { get; set; }
    }

    public interface IRecaptchaService
    {
        Task<bool> Validate(string token);
    }

    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string Timestamp { get; set; }

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
        /// Upon validation, compares response score against required confidence threshold as defined in settings.
        /// </summary>
        /// <param name="token">Client response token</param>
        /// <returns>Confirmation status as boolean</returns>
        public async Task<bool> Validate(string token)
        {
            // Create a new recaptcha verification request payload
            Dictionary<string, string> request = new Dictionary<string, string>()
            {
                { "secret", _settings.SecretKey},
                { "response", token }
            };

            // Turn the request payload string into a HttpContent instance
            var content = new FormUrlEncodedContent(request);

            // Perform the request
            var clientResponse = await _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

            // Convert the response information into a RecaptchaResponse for processing
            var responseOb = JsonSerializer.Deserialize<RecaptchaResponse>(await clientResponse.Content.ReadAsStringAsync());

            // Determine if the confidence interval is met
            return responseOb.Score > _settings.ConfidenceThreshold;
        }
    }
}