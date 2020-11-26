using System.Text.Json.Serialization;

namespace patrons_web_api.Models.Transfer.Response
{
    public class ManagerResponse
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("isPasswordReset")]
        public bool IsPasswordReset { get; set; }
    }
}