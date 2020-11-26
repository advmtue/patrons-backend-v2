using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace patrons_web_api.Models.Transfer.Request
{
    public class ManagerLoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}