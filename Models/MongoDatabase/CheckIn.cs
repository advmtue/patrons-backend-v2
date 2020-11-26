using System.Reflection.Metadata.Ecma335;
using System.Net.Cache;
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using patrons_web_api.Models.Transfer.Request;

namespace patrons_web_api.Models.MongoDatabase
{
    public abstract class CheckInBase
    {
        [BsonElement("time")]
        public long Time { get; set; }
    }

    public class CheckIn : CheckInBase
    {
        [BsonElement("people")]
        public List<DiningPatron> People { get; set; }
    }

    public class CheckInDocument : CheckInBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("people")]
        public List<DiningPatronDocument> People { get; set; }

        public static CheckInDocument FromCheckInRequest(DiningCheckInRequest request)
        {
            CheckInDocument newDocument = new CheckInDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                People = new List<DiningPatronDocument>()
            };

            request.People.ForEach(patron =>
            {
                newDocument.People.Add(DiningPatronDocument.FromPatron(patron));
            });

            return newDocument;
        }
    }
}