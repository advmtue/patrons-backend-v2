using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Patrons.CheckIn.API.Services
{
    public class RecaptchaWebSettings : IRecaptchaWebSettings
    {
        /// <summary>
        /// Secret key as distributed by Google Console.
        /// </summary>
        public string SecretKey { get; set; }
    }

    public interface IRecaptchaWebSettings
    {
        string SecretKey { get; set; }
    }

    public interface IRecaptchaWebService {
        public Task<RecaptchaResponse> Get(string token);
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

    public class RecaptchaWebService : IRecaptchaWebService {
        private readonly IRecaptchaWebSettings _settings;
        private readonly HttpClient _client;

        public RecaptchaWebService(
                IRecaptchaWebSettings settings,
                HttpClient httpClient)
        {
            _settings = settings;
            _client = httpClient;
        }

        /// <summary>
        /// Exchange a token with the recaptcha V3 API for information regarding the request's likelihood
        /// of being automated by a bot.
        /// </summary>
        /// <param name="token">Ephemeral recaptcha token</param>
        public async Task<RecaptchaResponse> Get(string token)
        {
            // Thrown an ArgumentNullException if token is null.
            if (token == null) throw new ArgumentNullException();

            // Create request payload.
            var content = new FormUrlEncodedContent(
                    new Dictionary<string, string> {
                        { "secret", _settings.SecretKey },
                        { "response", token }
                    }
            );

            // Make the request to the recaptcha V3 API.
            var clientResponse = await _client.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    content);

            // Deserialize the response into a RecaptchaResponse object and return it.
            return JsonSerializer.Deserialize<RecaptchaResponse>(await clientResponse.Content.ReadAsStringAsync());
        }
    }
}
