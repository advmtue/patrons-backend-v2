using System;
using System.Collections.Generic;
using Xunit;
using Moq;

using Patrons.CheckIn.API.Models.MongoDatabase;
using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Services;
using Patrons.CheckIn.API.Controllers;

namespace Patrons.CheckIn.API.Tests.Services {
    public class ManagerServiceTests
    {
        // Some defaults so that they don't need to be redefined in every test.
        // Only use in tests where the value doesn't really matter, and that you only check equality.
        public static readonly string _managerId = "managerId";
        public static readonly string _serviceId = "serviceId";
        public static readonly string _tableId = "tableId";
        public static readonly string _checkInId = "checkInId";
        public static readonly string _patronId = "patronId";
        public static readonly string _tableNumber = "tableNumber";

        /// <summary>
        /// Test that providing null arguments to the construct yields a non-null instance.
        /// </summary>
        [Fact]
        public static void Constructor_Creates()
        {
            // Act:
            // Create a new manager service.
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure the managerService is not a null instance.
            Assert.NotNull(managerService);
        }

        [Theory]
        // Null login request object, null IP.
        [InlineData(true, null, null, null)]
        // Null login request object, valid IP.
        [InlineData(true, null, null, "127.0.0.1")]
        // Non-null login request object, null username, null password, null IP.
        [InlineData(false, null, null, null)]
        // Non-null login request object, valid username, null password, null IP.
        [InlineData(false, "testuser", null, null)]
        // Non-null login request object, null username, valid password, null IP.
        [InlineData(false, null, "testpassword", null)]
        // Non-null login request object, valid username, valid password, null IP.
        [InlineData(false, "testuser", "testpassword", null)]
        // Non-null login request object, null username, null password, valid IP.
        [InlineData(false, null, null, "127.0.0.1")]
        // Non-null login request object, valid username, null password, valid IP.
        [InlineData(false, "testuser", null, "127.0.0.1")]
        // Non-null login request object, null username, valid password, valid IP.
        [InlineData(false, null, "testpassword", "127.0.0.1")]
        public static async void Login_NullParams_YieldsNotNull(
                bool isLoginNull,
                string user,
                string pwd,
                string ip)
        {
            // Arrange:
            // Build manager login information.
            ManagerLoginRequest managerRequest = null;

            // If the login should not be null in this test, fill out the desired fields.
            if (!isLoginNull) {
                managerRequest = new ManagerLoginRequest
                {
                    Username = user,
                    Password = pwd
                };
            }

            // Create a manager service.
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure that ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => managerService.Login(managerRequest, ip));
        }

        /// <summary>
        /// Test that a BadLoginException is thrown when supplying invalid login credentials.
        /// Should throw if the re
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async void Login_BadLogin(bool isPasswordReset)
        {
            // Arrange:
            // Mock password service.
            var mockPwdService = new Mock<IPasswordService>();
            // Password service returns a bad password match for IsPasswordMatch().
            mockPwdService.Setup(p => p.IsPasswordMatch(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns(false);

            // Mock database.
            var mockDb = new Mock<IPatronsDatabase>();
            // Returned manager from GetManagerByUsername.
            var manager = new ManagerDocument {
                Username = "username",
                Password = "password",
                Salt = "salt",
                IsPasswordReset = isPasswordReset
            };
            mockDb.Setup(db => db.GetManagerByUsername(It.IsAny<string>()).Result).Returns(manager);

            // Create login information so that a ArgumentNullException isn't thrown.
            // loginInfo.Password should not be the same password as manager.Password.
            var loginInfo = new ManagerLoginRequest
            {
                Username = "username",
                Password = "wrongpassword"
            };
            string ipAddress = "ipaddress";

            // Create a manager service.
            var managerService = new ManagerService(mockDb.Object, mockPwdService.Object, null);

            // Assert:
            // Ensure a BadLoginException is thrown.
            await Assert.ThrowsAsync<BadLoginException>(
                    () => managerService.Login(loginInfo, ipAddress));
        }

        /// <summary>
        /// Test that the session details which are created represent a session for the supplied manager.
        /// Also test that the returned ManagerLoginResponse has the correct information assigned.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async void Login_SessionDetailsAndLoginResponse_AreCorrect(bool isPasswordReset)
        {
            // Arrange:
            // Predefined values.
            string sessionId = "thisisthesessionID";
            string managerId = "thisisthemanagerID";
            string ipAddress = "127.0.0.1";
            string password = "password";
            string username = "username";

            // Build login information.
            ManagerLoginRequest loginInfo = new ManagerLoginRequest {
                Username = username,
                Password = password
            };

            // Mock database.
            var mockDb = new Mock<IPatronsDatabase>();

            var manager = new ManagerDocument {
                Id = managerId,
                Username = username,
                Password = password,
                Salt = "salt",
                IsPasswordReset = isPasswordReset
            };

            // Database returns predefined manager.
            mockDb.Setup(db => db.GetManagerByUsername(It.IsAny<string>()).Result).Returns(manager);

            // Mock pwd service.
            var mockPwd = new Mock<IPasswordService>();

            // Force password match.
            mockPwd.Setup(pwd => pwd.IsPasswordMatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            // Mock session service.
            var mockSession = new Mock<ISessionService>();
            // Ensure "generated" sessionID is predictable.
            mockSession.Setup(s => s.GenerateSessionId().Result).Returns(sessionId);

            // Create a manager service.
            var managerService = new ManagerService(mockDb.Object, mockPwd.Object, mockSession.Object);

            // Act:
            var loginResponse = await managerService.Login(loginInfo, ipAddress);

            // Assert:
            Assert.Equal(loginResponse.SessionId, sessionId);
            Assert.Equal(loginResponse.AccessLevel, isPasswordReset ? "RESET" : "FULL");

            // Ensure the session has the correct information when saved to the database.
            mockDb.Verify(
                db => db.SaveSession(
                    It.Is<SessionDocument>(
                        session =>
                            session.SessionId == sessionId
                            && session.ManagerId == managerId
                            && session.IPAddress == ipAddress
                            && session.AccessLevel == (isPasswordReset ? "RESET" : "FULL")
                            && session.IsActive == true
                    )
                )
            );
        }

        /// <summary>
        /// Test that passing a null manager ID to manager.GetSelf throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public static async void GetSelf_NullManagerId_ThrowsArgumentNull()
        {
            // Arrange:
            // Create a manager service.
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure that passing a null parameter to GetSelf throws an ArgumentNullException.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => managerService.GetSelf(null));
        }

        /// <summary>
        /// Ensure that a ManagerNotFoundException is thrown when supplying and invalid manager Id.
        /// </summary>
        [Fact]
        public static async void GetSelf_InvalidManager_ThrowsManagerNotFound()
        {
            // Arrange:
            string managerId = "managerId";

            // Mock database.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.GetManagerById(managerId)).ThrowsAsync(new ManagerNotFoundException());

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            // Ensure a ManagerNotFoundException is thrown.
            await Assert.ThrowsAsync<ManagerNotFoundException>(
                    () => managerService.GetSelf(managerId));
        }

        /// <summary>
        /// Ensure that the content of the ManagerResponse returned from GetSelf contains correctly assigned
        /// information from the database.
        /// </summary>
        [Fact]
        public static async void GetSelf_ValidManager_ValidResponse()
        {
            // Arrange:
            string managerId = "managerId";

            var manager = new ManagerDocument {
                FirstName = "firstname",
                LastName = "lastname",
                Email = "manager@manager.manager",
                IsPasswordReset = false
            };

            // Mock database
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.GetManagerById(managerId).Result).Returns(manager);

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Act:
            // Save managerLoginResponse output from calling GetSelf.
            var result = await managerService.GetSelf(managerId);

            // Assert:
            Assert.Equal(result.FirstName, manager.FirstName);
            Assert.Equal(result.LastName, manager.LastName);
            Assert.Equal(result.Email, manager.Email);
            Assert.Equal(result.IsPasswordReset, manager.IsPasswordReset);
        }

        /// <summary>
        /// Ensure that any null combination of managerId and newPassword throws an ArgumentNullException.
        /// </summary>
        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "password")]
        [InlineData("managerId", null)]
        public static async void UpdatePassword_NullParams_ThrowsArgumentNull(string managerId, string pwd)
        {
            // Arrange:
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure an ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.UpdatePassword(managerId, pwd));
        }

        /// <summary>
        /// When passing an invalid manager ID (a manager not found) expect that UpdatePassword also throws
        /// a ManagerNotFoundException.
        /// </summary>
        [Fact]
        public static async void UpdatePassword_InvalidManager_ThrowsManagerNotFound()
        {
            // Arrange:
            string managerId = "managerId";

            // Mock database.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.GetManagerById(managerId)).ThrowsAsync(new ManagerNotFoundException());

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            // Ensure a ManagerNotFoundException is thrown.
            await Assert.ThrowsAsync<ManagerNotFoundException>(
                    () => managerService.UpdatePassword(managerId, "password"));
        }

        /// <summary>
        /// Ensure that the database is updated with new password information.
        /// Ensure that manager sessions are all deactivated.
        /// </summary>
        [Fact]
        public static async void UpdatePassword_ValidManager_Database_Calls()
        {
            // Arrange:
            string password = "password";

            // Predefine manager.
            var manager = new ManagerDocument {
                Id = password
            };

            // Predefine new password.
            var passwordInfo = new HashedPasswordWithSalt {
                HashedPassword = "password",
                Salt = "salt"
            };

            // Mock database.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.GetManagerById(manager.Id).Result).Returns(manager);

            // Mock password service.
            var mockPwd = new Mock<IPasswordService>();
            mockPwd.Setup(pwd => pwd.CreatePassword(password)).Returns(passwordInfo);

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, mockPwd.Object, null);

            // Act:
            // Update the manager's password.
            await managerService.UpdatePassword(manager.Id, password);

            // Assert:
            // Ensure that db.ManagerUpdatePassword is called with correct information.
            mockDb.Verify(
                db => db.ManagerUpdatePassword(manager.Id, passwordInfo.HashedPassword, passwordInfo.Salt),
                Times.Once());

            // Ensure that db.ManagerDeactivateSessions is called for the manager ID.
            mockDb.Verify(
                db => db.ManagerDeactivateSessions(manager.Id),
                Times.Once());
        }

        /// <summary>
        /// Ensure that passing a null manager ID throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public static async void GetVenues_NullManagerId_ThrowsArgumentNull()
        {
            // Arrange:
            // Create a new managerService.
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure an ArgumentNullException is thrown when passing a null managerId.
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.GetVenues(null));
        }

        /// <summary>
        /// Ensure a lookup for manager venues is performed against the database.
        /// </summary>
        [Fact]
        public static async void GetVenues_ValidManager_CallsDatabaseLookup()
        {
            // Arrange:
            var managerId = "managerId";
            var mockDb = new Mock<IPatronsDatabase>();
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Act:
            await managerService.GetVenues(managerId);

            // Assert:
            // Ensure that db.GetManagerVenues was called for the managerId.
            mockDb.Verify(db => db.GetManagerVenues(managerId), Times.Once());
        }

        /// <summary>
        /// Ensure that any combination of null parameters throws an ArgumentNullException.
        /// </summary>
        [Theory]
        // All permutations would be 2^5 = 32 which is too much effort to InlineData.
        // We only really care that it fails if any of the arguments is null anyway.
        [InlineData(null, "serviceId", "tableId", "checkInId", "patronId")]
        [InlineData("managerId", null, "tableId", "checkInId", "patronId")]
        [InlineData("managerId", "serviceId", null, "checkInId", "patronId")]
        [InlineData("managerId", "serviceId", "tableId", null, "patronId")]
        [InlineData("managerId", "serviceId", "tableId", "checkInId", null)]
        public static async void DeleteDiningPatron_NullParams_ThrowsArgumentNull(
                string managerId,
                string serviceId,
                string tableId,
                string checkInId,
                string patronId)
        {
            // Arrange:
            var managerService = new ManagerService(null, null, null);

            // Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.DeleteDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId));
        }

        /// <summary>
        /// Ensure that a NoAccessException is thrown when a manager does not have sufficient access to
        /// delete a dining patron from a service.
        /// </summary>
        [Fact]
        public static async void DeleteDiningPatron_NoAccess_ThrowsNoAccess()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var mockDb = new Mock<IPatronsDatabase>();
            // ManagerCanAccessService returns false.
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(false);

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert;
            await Assert.ThrowsAsync<NoAccessException>(
                () => managerService.DeleteDiningPatron(managerId, serviceId, tableId, checkInId, patronId));
        }

        /// <summary>
        /// Ensure that a database update request is performed when the requesting manager has access to
        /// delete dining patrons from the specified service.
        /// </summary>
        [Fact]
        public static async void DeleteDiningPatron_HasAccess_UpdatesDatabase()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            // Mock database and set ManagerCanAccessService(managerId, serviceId) to return TRUE.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Act:
            await managerService.DeleteDiningPatron(managerId, serviceId, tableId, checkInId, patronId);

            // Assert:
            // Ensure that a database update was called.
            mockDb.Verify(
                db => db.DeleteDiningPatron(serviceId, tableId, checkInId, patronId));
        }

        /// <summary>
        /// Ensure that an ArgumentNullException is thrown if any parameters to UpdateDiningPatron is null.
        /// Also ensure that an ArgumentNullException is thrown if FirstName or Phonenumber are null for the
        /// updated patron information.
        /// </summary>
        [Theory]
        // Manager ID is null.
        [InlineData(null, "serviceId", "tableId", "checkInId", "patronId", false)]
        // Service ID is null.
        [InlineData("managerId", null, "tableId", "checkInId", "patronId", false)]
        // Table ID is null.
        [InlineData("managerId", "serviceId", null, "checkInId", "patronId", false)]
        // Check-in ID is null.
        [InlineData("managerId", "serviceId", "tableId", null, "patronId", false)]
        // Patron ID is null.
        [InlineData("managerId", "serviceId", "tableId", "checkInId", null, false)]
        // DiningPatronUpdateRequest is null.
        [InlineData("managerId", "serviceId", "tableId", "checkInId", "patronId", true)]
        public static async void UpdateDiningPatron_AnyNullParams_ThrowsArgumentNull(
                string managerId,
                string serviceId,
                string tableId,
                string checkInId,
                string patronId,
                bool isDiningPatronNull)
        {
            // Arrange:
            string diningPatronFirstName = "firstname";
            string diningPatronPhoneNumber = "phonenumber";

            // Create a new manager service
            var managerService = new ManagerService(null, null, null);

            // Create DiningPatronUpdate request if it should not be the null parameter for this test case.
            DiningPatronUpdateRequest updateInfo = null;
            if (!isDiningPatronNull)
            {
                updateInfo = new DiningPatronUpdateRequest
                {
                    FirstName = diningPatronFirstName,
                    PhoneNumber = diningPatronPhoneNumber,
                };
            }

            // Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// Test that if a manager does not have sufficient access that a NoAccessException is thrown.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_NoAccess_ThrowsNoAccess()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber"
            };

            // Mock that ManagerCanAccessService returns false.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(false);

            // Create a new manager service.
            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            // Assert that a NoAccessException is thrown.
            await Assert.ThrowsAsync<NoAccessException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When the database throws a ServiceNotFoundException, ensure that it is also thrown by
        /// the manager service.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_ServiceNotFound_ThrowsServiceNotFound()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber"
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Ensure that a ServiceNotFoundException is thrown.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId)).ThrowsAsync(new ServiceNotFoundException());

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When the dining service is not active ensure that a ServiceIsNotActiveException is thrown.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_NotActive_ThrowsServiceIsNotActive()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber",
            };

            // A dining service which is not active.
            var diningService = new DiningServiceDocument
            {
                IsActive = false,
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Return the dining service.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<ServiceIsNotActiveException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When there is no matching sitting ensure that a TableNotFoundException is thrown.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_TableNotFound_ThrowsTableNotFound()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber",
            };

            // A dining service which is not active.
            var diningService = new DiningServiceDocument
            {
                IsActive = true,
                Sittings = new List<SittingDocument>(),
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Return the dining service.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<TableNotFoundException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When there is no matching checkIn ensure that a CheckInNotFoundExcption is thrown.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_CheckInNotFound_ThrowsCheckInNotFound()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber",
            };

            // Matching table document containing no matching checkIns.
            var table = new SittingDocument
            {
                Id = tableId,
                CheckIns = new List<CheckInDocument>(),
            };

            // Matching dining service document.
            var diningService = new DiningServiceDocument
            {
                IsActive = true,
                Sittings = new List<SittingDocument> { table },
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Return the dining service.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<CheckInNotFoundExcption>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When there is no matching patron ensure that a PatronNotFoundException is thrown.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_NoPatron_ThrowsPatronNotFound()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber",
            };

            // Check-in document with no matching patrons.
            var checkIn = new CheckInDocument
            {
                Id = checkInId,
                People = new List<DiningPatronDocument>(),
            };

            // Matching table document containing matching check-in.
            var table = new SittingDocument
            {
                Id = tableId,
                CheckIns = new List<CheckInDocument> { checkIn },
            };

            // Matching dining service document containing matching table.
            var diningService = new DiningServiceDocument
            {
                IsActive = true,
                Sittings = new List<SittingDocument> { table },
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Return the dining service.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<PatronNotFoundException>(
                () => managerService.UpdateDiningPatron(
                    managerId, serviceId, tableId, checkInId, patronId, updateInfo));
        }

        /// <summary>
        /// When all information matches, ensure that the database update is called with the correct
        /// new information, as provided in the parameters.
        /// </summary>
        [Fact]
        public static async void UpdateDiningPatron_Valid_UpdatesPatron()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string patronId = "patronId";

            var updateInfo = new DiningPatronUpdateRequest
            {
                FirstName = "firstname",
                PhoneNumber = "phonenumber",
            };

            // Matching patron document.
            var patron = new DiningPatronDocument
            {
                Id = patronId,
                FirstName = "oldfirstname",
                PhoneNumber = "oldphonenumber",
            };

            // Check-in document containing matching patron.
            var checkIn = new CheckInDocument
            {
                Id = checkInId,
                People = new List<DiningPatronDocument> { patron },
            };

            // Matching table document containing matching check-in.
            var table = new SittingDocument
            {
                Id = tableId,
                CheckIns = new List<CheckInDocument> { checkIn },
            };

            // Matching dining service document containing matching table.
            var diningService = new DiningServiceDocument
            {
                IsActive = true,
                Sittings = new List<SittingDocument> { table },
            };

            // Mock the database so that the manager has access to the service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(true);
            // Return the dining service.
            mockDb.Setup(db => db.GetDiningServiceById(serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Act:
            await managerService.UpdateDiningPatron(managerId, serviceId, tableId, checkInId, patronId, updateInfo);

            // Assert:
            // Ensure that the database call has been performed once to update the patron with new information.
            mockDb.Verify(
                db => db.UpdateDiningPatron(serviceId, tableId, checkInId, patronId,
                    It.Is<DiningPatronDocument>(
                        p => p.Id == patronId
                        && p.FirstName == updateInfo.FirstName
                        && p.PhoneNumber == updateInfo.PhoneNumber)),
                Times.Once());
        }

        /// <summary>
        /// If any null parameters are passed to MoveDiningGroup, expect an ArgumentNullException.
        /// </summary>
        [Theory]
        [InlineData(null, "serviceId", "tableId", "checkInId", "newTableNumber")]
        [InlineData("managerId", null, "tableId", "checkInId", "newTableNumber")]
        [InlineData("managerId", "serviceId", null, "checkInId", "newTableNumber")]
        [InlineData("managerId", "serviceId", "tableId", null, "newTableNumber")]
        [InlineData("managerId", "serviceId", "tableId", "checkInId", null)]
        public static async void MoveDiningGroup_AnyNullParams_ThrowsArgumentNull(
            string managerId,
            string serviceId,
            string tableId,
            string checkInId,
            string newTableNumber)
        {
            // Arrange:
            var managerService = new ManagerService(null, null, null);

            // Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.MoveDiningGroup(managerId, serviceId, tableId, checkInId, newTableNumber));
        }

        /// <summary>
        /// Test that when a manager does not have access to move a dining group, a NoAccessException is thrown.
        /// </summary>
        [Fact]
        public static async void MoveDiningGroup_NoAccess_ThrowsNoAccess()
        {
            // Arrange:
            string managerId = "managerId";
            string serviceId = "serviceId";
            string tableId = "tableId";
            string checkInId = "checkInId";
            string newTableNumber = "newTableNumber";

            // Database should return that the manager does not have access to this service.
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(managerId, serviceId).Result).Returns(false);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<NoAccessException>(
                () => managerService.MoveDiningGroup(managerId, serviceId, tableId, checkInId, newTableNumber));
        }

        /// <summary>
        /// When the specified service is not active, ensure a ServiceIsNotActiveException is thrown.
        /// </summary>
        [Fact]
        public static async void MoveDiningGroup_NotActive_ThrowsNotActive()
        {
            // Arrange
            var diningService = new DiningServiceDocument
            {
                Id = _serviceId,
                IsActive = false,
            };

            // Mock the database.
            var mockDb = new Mock<IPatronsDatabase>();
            // Return that the manager has access to the service.
            mockDb.Setup(db => db.ManagerCanAccessService(_managerId, _serviceId).Result).Returns(true);
            // Return the predefined dining service document.
            mockDb.Setup(db => db.GetDiningServiceById(diningService.Id).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            // Ensure the manager service throws a ServiceIsNotActiveException.
            await Assert.ThrowsAsync<ServiceIsNotActiveException>(
                () => managerService.MoveDiningGroup(_managerId, _serviceId, _tableId, _checkInId, "000"));
        }

        /// <summary>
        /// When providing valid information to MoveDiningGroup, ensure that a database call is performed.
        /// </summary>
        [Fact]
        public static async void MoveDiningGroup_Valid_CallsDatabase()
        {
            // Arrange:
            var diningService = new DiningServiceDocument
            {
                Id = _serviceId,
                IsActive = true,
            };

            var mockDb = new Mock<IPatronsDatabase>();
            // Manager can access this service.
            mockDb.Setup(db => db.ManagerCanAccessService(_managerId, _serviceId).Result).Returns(true);
            // Return a dining service which is active, so to not throw any exceptions.
            mockDb.Setup(db => db.GetDiningServiceById(_serviceId).Result).Returns(diningService);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Act:
            // Move the dining group.
            await managerService.MoveDiningGroup(_managerId, _serviceId, _tableId, _checkInId, _tableNumber);

            // Assert:
            // Ensure the database update has been called with the correct information.
            mockDb.Verify(
                db => db.MoveDiningGroup(_serviceId, _tableId, _checkInId, _tableNumber),
                Times.Once());
        }

        /// <summary>
        /// When providing arguments to MoveDiningTable, expect that it should throw an ArgumentNullException
        /// if any of the parameters are null.
        /// </summary>
        [Theory]
        [InlineData(null, "serviceId", "tableId", "newTableNumber")]
        [InlineData("managerId", null, "tableId", "newTableNumber")]
        [InlineData("managerId", "serviceId", null, "newTableNumber")]
        [InlineData("managerId", "serviceId", "tableId", null)]
        public static async void MoveDiningTable_AnyNullParams_ThrowsArgumentNull(
            string managerId,
            string serviceId,
            string tableId,
            string newTableNumber)
        {
            // Arrange:
            var managerService = new ManagerService(null, null, null);

            // Assert:
            // Ensure an ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => managerService.MoveDiningTable(managerId, serviceId, tableId, newTableNumber));
        }

        /// <summary>
        /// If a manager does not have access to move a dining table, ensure a NoAccessException is thrown.
        /// </summary>
        [Fact]
        public static async void MoveDiningTable_NoAccess_ThrowsNoAccess()
        {
            // Arrange:
            var mockDb = new Mock<IPatronsDatabase>();
            mockDb.Setup(db => db.ManagerCanAccessService(_managerId, _serviceId).Result).Returns(false);

            var managerService = new ManagerService(mockDb.Object, null, null);

            // Assert:
            await Assert.ThrowsAsync<NoAccessException>(
                () => managerService.MoveDiningTable(_managerId, _serviceId, _tableId, _tableNumber));
        }
    }
}
