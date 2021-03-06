using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public class GamingPatronDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // --- Contact information
        // First name
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        // Last name (Surname)
        [BsonElement("lastName")]
        public string LastName { get; set; }

        // Phone Number
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        // Time of check-in
        [BsonElement("checkInTime")]
        public long CheckInTime { get; set; }

        // Time of check-out, or -1 if still active
        [BsonElement("checkOutTime")]
        public long CheckOutTime { get; set; }

        // Is the patron still active in the venue? Determined by Gaming Marshall
        [BsonElement("isActive")]
        public bool IsActive { get; set; }
    }
}
