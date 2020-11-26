using System.Text.Json.Serialization;

namespace patrons_web_api.Models.Transfer.Request
{
    public class UpdatePasswordRequest
    {
        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }
}