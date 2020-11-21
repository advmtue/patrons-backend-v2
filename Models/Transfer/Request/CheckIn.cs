using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Models.Transfer.Request
{
    public class DiningCheckInRequest
    {
        [Required]
        [JsonPropertyName("people")]
        public List<DiningPatron> People { get; set; }

        [Required]
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
    }

    public class GamingCheckInRequest : GamingPatron { }
}