using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public class GamingPatron
    {
        // Required contact information
        [Required]
        [BsonElement("firstName")]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [Required]
        [BsonElement("lastName")]
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [Required]
        [BsonElement("phoneNumber")]
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class GamingPatronDocument : GamingPatron
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id;

        // ID of corresponding service
        [BsonElement("serviceId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ServiceId;

        // Status within the venue
        [BsonElement("checkInTime")]
        public long CheckInTime;

        [BsonElement("checkOutTime")]
        public long CheckOutTime;

        [BsonElement("isActive")]
        public bool IsActive;
    }
}