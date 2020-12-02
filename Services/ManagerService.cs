using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using MongoDB.Bson;

using patrons_web_api.Controllers;
using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class ManagerLoginResponse
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; }
    }

    public interface IManagerService
    {
        // Manager actions
        Task<ManagerLoginResponse> Login(ManagerLoginRequest loginRequest, string ipAddress);
        Task<List<ManagerVenueDocument>> GetVenues(string managerId);
        Task<ManagerResponse> GetSelf(string managerId);
        Task UpdatePassword(string managerId, string newPassword);

        // Service actions
        Task<DiningServiceDocument> StartDiningService(string managerId, string venueId, string areaId);
        Task<GamingServiceDocument> StartGamingService(string managerId, string venueId, string areaId);
        Task StopDiningService(string managerId, string venueId, string areaId);
        Task StopGamingService(string managerId, string venueId, string areaId);

        // Patron actions
        Task DeleteDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId);
        Task UpdateDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId, DiningPatronUpdateRequest update);

        // Dining group actions
        Task<string> MoveDiningGroup(string managerId, string serviceId, string tableId, string checkInIdx, string newTableNumber);

        // Dining table actions
        Task<string> MoveDiningTable(string managerId, string serviceId, string tableId, string newTableNumber);
        Task CloseDiningTable(string managerId, string serviceId, string tableId);

        // Gaming patron actions
        Task DeleteGamingPatron(string managerId, string serviceId, string patronId);
        Task UpdateGamingPatron(string managerId, string serviceId, string patronId, GamingPatronUpdateRequest update);
        Task CheckOutGamingPatron(string managerId, string serviceId, string patronId);

    };

    public class ManagerService : IManagerService
    {
        private IPatronsDatabase _database;
        private PasswordService _password;
        private ISessionService _session;

        public ManagerService(IPatronsDatabase db, PasswordService pwd, ISessionService session)
        {
            _database = db;
            _password = pwd;
            _session = session;
        }

        private async Task _EnsureManagerCanAccessVenue(string managerId, string venueId)
        {
            bool managerCanAccess = await _database.ManagerCanAccessVenue(managerId, venueId);

            if (!managerCanAccess) throw new NoAccessException();
        }

        private async Task _EnsureManagerCanAccessService(string managerId, string serviceId)
        {
            bool managercanAccess = await _database.ManagerCanAccessService(managerId, serviceId);

            if (!managercanAccess) throw new NoAccessException();
        }

        public async Task<ManagerLoginResponse> Login(ManagerLoginRequest loginInfo, string ipAddress)
        {
            // Pull manager information
            var manager = await _database.GetManagerByUsername(loginInfo.Username);

            bool canLogIn = false;

            if (manager.IsPasswordReset && loginInfo.Password == manager.Password)
            {
                // If the manager has a password reset and the correct reset password is provided
                canLogIn = true;
            }
            else if (_password.IsPasswordMatch(loginInfo.Password, manager.Salt, manager.Password))
            {
                // If the manager password + salt combination matches the stored password
                canLogIn = true;
            }

            // If the logins were a failure, throw an exception
            if (!canLogIn)
            {
                throw new BadLoginException();
            }

            // Create a new session
            var newSession = new SessionDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                SessionId = await _session.GenerateSessionId(),
                ManagerId = manager.Id,
                IPAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AccessLevel = manager.IsPasswordReset ? "RESET" : "FULL",
                IsActive = true
            };

            // Save new session
            await _database.SaveSession(newSession);

            return new ManagerLoginResponse
            {
                SessionId = newSession.SessionId,
                AccessLevel = newSession.AccessLevel
            };
        }

        public async Task<ManagerResponse> GetSelf(string managerId)
        {
            var manager = await _database.GetManagerById(managerId);

            return new ManagerResponse
            {
                FirstName = manager.FirstName,
                LastName = manager.LastName,
                Email = manager.Email,
                IsPasswordReset = manager.IsPasswordReset
            };
        }

        public async Task UpdatePassword(string managerId, string newPassword)
        {
            // Pull manager info
            var manager = await _database.GetManagerById(managerId);

            // Create a new password hash using the original manager salt
            var newPassHash = _password.RegeneratePassword(newPassword, manager.Salt);

            // Save new password
            await _database.ManagerUpdatePassword(manager.Id, newPassHash.HashedPassword);

            // Deactivate all manager sessions
            await _database.ManagerDeactivateSessions(manager.Id);
        }

        public async Task<List<ManagerVenueDocument>> GetVenues(string managerId)
        {
            return await _database.GetManagerVenues(managerId);
        }

        public async Task DeleteDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId)
        {
            // Ensure manager has access to the given venue
            await _EnsureManagerCanAccessService(managerId, serviceId);

            await _database.DeleteDiningPatron(serviceId, tableId, checkInId, patronId);
        }

        public async Task UpdateDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId, DiningPatronUpdateRequest update)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            DiningServiceDocument diningService = await _database.GetDiningServiceById(serviceId);

            // Check that the service is active
            if (!diningService.IsActive) throw new ServiceIsNotActiveException();

            // Locate table
            var table = diningService.Sittings.Find(sitting => sitting.Id.Equals(tableId));
            if (table == null) throw new TableNotFoundException();

            // Locate checkIn
            var checkIn = table.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Locate patron
            var patron = checkIn.People.Find(p => p.Id.Equals(patronId));
            if (patron == null) throw new PatronNotFoundException();

            // Update patron
            patron.FirstName = update.FirstName;
            patron.PhoneNumber = update.PhoneNumber;

            // Save
            await _database.UpdateDiningPatron(serviceId, tableId, checkInId, patronId, patron);
        }

        public async Task<string> MoveDiningGroup(string managerId, string serviceId, string tableId, string checkInId, string newTableNumber)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Ensure the service is active
            var service = await _database.GetDiningServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            return await _database.MoveDiningGroup(serviceId, tableId, checkInId, newTableNumber);
        }

        public async Task<string> MoveDiningTable(string managerId, string serviceId, string tableId, string newTableNumber)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Ensure the service is active
            var service = await _database.GetDiningServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Move the table
            return await _database.MoveDiningTable(serviceId, tableId, newTableNumber);
        }

        public async Task CloseDiningTable(string managerId, string serviceId, string tableId)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Ensure service is active
            var service = await _database.GetDiningServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            await _database.CloseDiningTable(serviceId, tableId);
        }

        public async Task DeleteGamingPatron(string managerId, string serviceId, string patronId)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Ensure the service is active
            var service = await _database.GetGamingServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            await _database.DeleteGamingPatron(serviceId, patronId);
        }

        public async Task UpdateGamingPatron(string managerId, string serviceId, string patronId, GamingPatronUpdateRequest update)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Ensure the service is active
            var service = await _database.GetGamingServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            await _database.UpdateGamingPatron(managerId, patronId, update);
        }

        public async Task CheckOutGamingPatron(string managerId, string serviceId, string patronId)
        {
            await _EnsureManagerCanAccessService(managerId, serviceId);

            var service = await _database.GetGamingServiceById(serviceId);
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            await _database.CheckOutGamingPatron(serviceId, patronId);
        }

        public async Task<DiningServiceDocument> StartDiningService(string managerId, string venueId, string areaId)
        {
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Start the dining service and return it
            return await _database.StartDiningService(venueId, areaId);
        }

        public async Task<GamingServiceDocument> StartGamingService(string managerId, string venueId, string areaId)
        {
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Start the gaming service and return it
            return await _database.StartGamingService(venueId, areaId);
        }

        public async Task StopDiningService(string managerId, string venueId, string areaId)
        {
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Stop the dining service
            await _database.StopDiningService(venueId, areaId);
        }

        public async Task StopGamingService(string managerId, string venueId, string areaId)
        {
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Stop the gaming service
            await _database.StopGamingService(venueId, areaId);
        }
    }
}