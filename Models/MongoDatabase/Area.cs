using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public class AreaBase
    {
        [BsonElement("shortName")]
        public string ShortName { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("imgSrc")]
        public string ImgSrc { get; set; }

        [BsonElement("isOpen")]
        public bool IsOpen { get; set; }
    }

    public class AreaDocument : AreaBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }
}