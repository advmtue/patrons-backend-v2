using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public abstract class AreaBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // URL segment for area, such as patrons.at/{venueName}/{shortName}
        [BsonElement("shortName")]
        public string ShortName { get; set; }

        // Display name for area
        [BsonElement("name")]
        public string Name { get; set; }

        // Placeholder: Image source
        [BsonElement("imgSrc")]
        public string ImgSrc { get; set; }

        // Whether the area is currently accepting check-ins
        [BsonElement("isOpen")]
        public bool IsOpen { get; set; }

        public AreaBase() { }
        public AreaBase(AreaDocument area)
        {
            Id = area.Id;
            ShortName = area.ShortName;
            Name = area.Name;
            ImgSrc = area.ImgSrc;
            IsOpen = area.IsOpen;
        }
    }
    public class AreaDocument : AreaBase
    {
        // Current active service
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("activeService")]
        public string ActiveService { get; set; }
    }

    public class DiningAreaDocument : AreaBase
    {
        [BsonElement("activeService")]
        public DiningServiceDocument ActiveService { get; set; }

        public DiningAreaDocument(AreaDocument area) : base(area)
        {
            // Set activeService to null
            ActiveService = new DiningServiceDocument { Id = area.ActiveService };
        }
    }

    public class GamingAreaDocument : AreaBase
    {
        [BsonElement("activeService")]
        public GamingServiceDocument ActiveService { get; set; }

        public GamingAreaDocument(AreaDocument area) : base(area)
        {
            // Set activeService to null
            ActiveService = new GamingServiceDocument { Id = area.ActiveService };
        }
    }
}
