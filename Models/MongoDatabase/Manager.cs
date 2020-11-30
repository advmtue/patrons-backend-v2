using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public class ManagerDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // --- Contact information
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }


        // --- Login information
        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("salt")]
        public string Salt { get; set; }

        [BsonElement("isPasswordReset")]
        public bool IsPasswordReset { get; set; }

        // --- Access information
        [BsonElement("venueIds")]
        public List<ObjectId> VenueIds { get; set; }

    }
}