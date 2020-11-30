using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace patrons_web_api.Models.MongoDatabase
{
    public class DiningPatronDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [Required]
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
}