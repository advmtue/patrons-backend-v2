using System;
using Microsoft.Extensions.Logging;

using Xunit;
using Moq;

using Patrons.CheckIn.API.Services;
using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Models.MongoDatabase;

namespace Patrons.CheckIn.API.Tests.Services {
    public class NewsletterServiceTests {

        /// <summary>
        /// Test that the newsletter successfully instantiates with empty mock objects
        /// </summary>
        [Fact]
        public static void Constructor_Creates() {
            // Arrange:
            // Mock database and logger.
            var mockDb = new Mock<IPatronsDatabase>();
            var mockLogger = new Mock<ILogger<NewsletterService>>();

            // Act:
            // Create a newsletter service.
            var newsletterService = new NewsletterService(mockLogger.Object, mockDb.Object);

            // Assert:
            // Ensure the newsletter service is not null.
            Assert.NotNull(newsletterService);
        }

        /// <summary>
        /// Test that registering a new user with null email or name throws an ArgumentNullException.
        /// </summary>
        /// <param name="name">User name</param>
        /// <param name="email">User email address</param>
        [Theory]
        [InlineData(null, "testuser@email.com")]
        [InlineData("Test User", null)]
        [InlineData(null, null)]
        public static async void RegisterUser_NullNameOrEmail(string name, string email)
        {
            // Arrange:
            // Create a new newsletterService.
            var newsletterService = new NewsletterService(null, null);

            // Assert:
            // Ensure a ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => newsletterService.RegisterUser(name, email)
            );
        }

        /// <summary>
        /// Test to ensure that a MarketingUserAlreadySubscribedException is thrown when attempting to
        /// register an existing email address for marketing emails.
        /// </summary>
        [Fact]
        public static async void RegisterUser_AreadyRegistered()
        {
            // Arrange:
            // Mock database and logger.
            var mockDb = new Mock<IPatronsDatabase>();
            var mockLog = new Mock<ILogger<NewsletterService>>();

            // Empty marketing user to return in GetActiveMarketingUserByEmail.
            MarketingUser mUser = new MarketingUser() {
                Name = "Test User",
                Email = "testuser@email.com"
            };

            // Make the database return the existing user.
            mockDb.Setup(db => db.GetActiveMarketingUserByEmail(mUser.Email).Result).Returns(mUser);


            // Create a new newsletterService.
            var newsletterService = new NewsletterService(mockLog.Object, mockDb.Object);

            // Assert:
            // Expect the newsletter service to throw a MarketingUserAlreadySubscribedException.
            await Assert.ThrowsAsync<MarketingUserAlreadySubscribedException>(
                    () => newsletterService.RegisterUser(mUser.Name, mUser.Email)
            );
        }

        /// <summary>
        /// Test that inserting a new user calls db.CreateMarketingUser(...) if the user isn't already found
        /// in the database.
        /// </summary>
        [Fact]
        public static async void RegisterUser_NewUser()
        {
            // Arrange:
            // Mock database and logger.
            var mockDb = new Mock<IPatronsDatabase>();
            var mockLog = new Mock<ILogger<NewsletterService>>();

            // Create a user.
            MarketingUser mUser = new MarketingUser {
                Name = "Test User",
                Email = "testuser@email.com"
            };

            // Make the database mock throw a MarketingUserNotFoundException for user.
            mockDb.Setup(db => db.GetActiveMarketingUserByEmail(mUser.Email).Result).Throws(new MarketingUserNotFoundException());

            // Make the database mock return mUser when supplying mUser details to CreateMarketingUser.
            mockDb.Setup(db => db.CreateMarketingUser(mUser.Name, mUser.Email).Result).Returns(mUser);

            // Create a new newsletterService.
            var newsletterService = new NewsletterService(mockLog.Object, mockDb.Object);

            // Act:
            // Register a user.
            var newUser = await newsletterService.RegisterUser(mUser.Name, mUser.Email);

            // Assert:
            // Ensure that CreateMarketingUser is called at once.
            mockDb.Verify(db => db.CreateMarketingUser(mUser.Name, mUser.Email), Times.Once());
        }

        /// <summary>
        /// Test that an ArgumentNullException is thrown when providing a null unsubscribeId.
        /// </summary>
        [Fact]
        public static async void UnsubscribeFromMarketing_NullUnsubscribeId()
        {
            // Arrange:
            // Create a new newsletterService.
            var newsletterService = new NewsletterService(null, null);

            // Assert:
            // Ensure an ArgumentNullException is thrown for null unsubscribeId.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => newsletterService.UnsubscribeFromMarketing(null)
            );
        }

        /// <summary>
        /// Test that db.UnsubscribeFromMarketing is called once for any ID.
        ///
        /// Since the service only currently does null checks and calls the DB, there isn't much else that
        /// can be tested for. Adding further tests for this method will likely result in doing more
        /// testing on the mock rather than the method.
        /// <summary>
        [Fact]
        public static async void UnsubscribeFromMarketing_Call()
        {
            // Arrange:
            const string unsubscribeId = "validID";

            // Mock the database.
            var mockDb = new Mock<IPatronsDatabase>();

            // Create a new newsletterService.
            var newsletterService = new NewsletterService(null, mockDb.Object);

            // Act:
            // Call the unsubscription function. Doesn't matter what the unsubscribeId is if it's not null.
            await newsletterService.UnsubscribeFromMarketing(unsubscribeId);

            // Assert:
            // Ensure that the database function was called.
            mockDb.Verify(db => db.UnsubscribeFromMarketing(unsubscribeId), Times.Once());
        }
    }
}
