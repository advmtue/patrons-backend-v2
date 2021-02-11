using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Database
{
    public class SessionBase
    {
        [BsonElement("sessionId")]
        public string SessionId { get; set; }

        [BsonElement("managerId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ManagerId { get; set; }

        [BsonElement("ipAddress")]
        public string IPAddress { get; set; }

        [BsonElement("createdAt")]
        public long CreatedAt { get; set; }

        [BsonElement("accessLevel")]
        public string AccessLevel { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }
    }

    public class SessionDocument : SessionBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }
}
