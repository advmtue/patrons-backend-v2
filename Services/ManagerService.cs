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
        Task<ManagerLoginResponse> Login(ManagerLoginRequest loginRequest, string ipAddress);
        Task<List<ManagerVenueDocument>> GetVenues(string managerId);
        Task<ManagerResponse> GetSelf(string managerId);
        Task UpdatePassword(string managerId, string newPassword);
        Task<string> StartService(string managerId, string venueId, string areaId);
        Task StopService(string managerId, string venueId, string areaId);
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

        public async Task<string> StartService(string managerId, string venueId, string areaId)
        {
            return await Task.FromResult("asdf");
        }

        public async Task StopService(string managerId, string venueId, string areaId)
        {
        }
    }
}