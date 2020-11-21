using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace patrons_web_api.Models.MongoDatabase
{
    public class DiningPatron
    {
        [Required]
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [Required]
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class DiningPatronDocument : DiningPatron
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public static DiningPatronDocument FromPatron(DiningPatron p)
        {
            return new DiningPatronDocument
            {
                Id = (new ObjectId()).ToString(),
                FirstName = p.FirstName,
                PhoneNumber = p.PhoneNumber
            };
        }
    }
}