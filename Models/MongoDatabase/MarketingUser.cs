using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public class MarketingUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("subscribed")]
        public bool Subscribed { get; set; }

        [BsonElement("created_at")]
        public long CreatedAt { get; set; }

        [BsonElement("unsubscribed_at")]
        public long UnsubscribedAt { get; set; }
    }
}
