using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using patrons_web_api.Database;

using MongoDB.Bson;

using patrons_web_api.Controllers;
using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Services
{
    public interface IPatronService
    {
        Task SubmitGamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn);
        Task SubmitDiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn);
    }

    public class PatronService : IPatronService
    {
        private IPatronsDatabase _database;

        public PatronService(IPatronsDatabase database)
        {
            _database = database;
        }

        public async Task SubmitGamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn)
        {
            // Check that the venue exists, the area exists, the area is open, the area type is gaming
            // This will throw an error if the venue doesn't exist
            var venue = await _database.GetVenueById(venueId);

            // Find an area that matches the requested areaId
            var area = venue.GamingAreas.Find(a => a.Id.Equals(areaId));

            // Existence check
            if (area == null)
            {
                throw new AreaNotFoundException();
            }

            // Check that area is open
            if (!area.IsOpen)
            {
                throw new AreaIsClosedException();
            }

            // Ensure that there is an active service
            if (area.ActiveService == "NONE")
            {
                throw new ServiceNotFoundException();
            }

            // Create a new GamingPatron document
            GamingPatronDocument patron = new GamingPatronDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = checkIn.FirstName,
                LastName = checkIn.LastName,
                PhoneNumber = checkIn.PhoneNumber,
                CheckInTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CheckOutTime = -1,
                IsActive = true
            };

            // Save check-in
            await _database.SaveGamingCheckIn(area.ActiveService, patron);
        }

        public async Task SubmitDiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn)
        {
            // Check that the venue exists, the area exists, the area is open, the area type is gaming
            // This will throw an error if the venue doesn't exist
            var venue = await _database.GetVenueById(venueId);

            // Find an area that matches the requested areaId
            var area = venue.DiningAreas.Find(a => a.Id == areaId);

            // Existence check
            if (area == null)
            {
                throw new AreaNotFoundException();
            }

            // Check that area is open
            if (!area.IsOpen)
            {
                throw new AreaIsClosedException();
            }

            // Ensure that there is an active service
            if (area.ActiveService == "NONE")
            {
                throw new ServiceNotFoundException();
            }

            // Create a check in
            CheckInDocument checkInDocument = new CheckInDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                People = new List<DiningPatronDocument>()
            };

            // Append patrons to the check-in
            checkIn.People.ForEach(person =>
            {
                checkInDocument.People.Add(new DiningPatronDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = person.FirstName,
                    PhoneNumber = person.PhoneNumber
                });
            });

            await _database.CreateOrAppendDiningCheckIn(area.ActiveService, checkIn.TableNumber, checkInDocument);
        }
    }
}