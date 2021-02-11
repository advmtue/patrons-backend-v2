using System;
using System.Threading;
using Xunit;
using Moq;

using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Services;
using Patrons.CheckIn.API.Models.MongoDatabase;


namespace Patrons.CheckIn.API.Test.Services
{
    public class EmailServiceTests
    {

        /// <summary>
        /// Test that providing null arguments to the email service constructor yields a non-null object.
        /// </summary>
        [Fact]
        public static void Constructor_Creates()
        {
            // Arrange:
            // Act:
            // Create a new emailService with null parameters.
            var emailService = new EmailService(null, null, null);

            // Assert:
            // Ensure the email service is not null.
            Assert.NotNull(emailService);
        }

        /// <summary>
        /// Test that providing a null argument to SendMarketWelcome throws a ArgumentNullException.
        /// </summary>
        [Fact]
        public static async void SendMarketWelcome_NullUser()
        {
            // Arrange:
            var emailService = new EmailService(null, null, null);

            // Act:
            // Assert:
            // Ensure an ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => emailService.SendMarketWelcome(null)
            );
        }

        /// <summary>
        /// Ensure that passing mUser to SendMarketWelcome with any null name/email fields throws
        /// an ArgumentNullException.
        /// </summary>
        /// <param name="name">User name</param>
        /// <param email="email">User email</param>
        [Theory]
        [InlineData("Test User", null)]
        [InlineData(null, "testuser@email.com")]
        [InlineData(null, null)]
        public static async void SendMarketWelcome_NullUserFields(string name, string email)
        {
            // Arrange:
            // Create a new emailService.
            var emailService = new EmailService(null, null, null);

            // Create an invalid marketing user.
            MarketingUser mUser = new MarketingUser
            {
                Name = name,
                Email = email
            };

            // Act:
            // Assert:
            // Ensure an ArgumentNullException is thrown.
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => emailService.SendMarketWelcome(mUser)
            );
        }

        /// <summary>
        /// Test that SendTemplatedEmailAsync is called and with the correct information for valid params.
        /// </summary>
        [Theory]
        [InlineData("Test Patron", "testpatron@patron.com")]
        public static async void SendMarketWelcome_Valid(string name, string email)
        {
            // Arrange:
            string expectedUnsubscribeLink = ":)";

            MarketingUser mUser = new MarketingUser
            {
                Name = name,
                Email = email
            };

            // Mock the database and emailClient.
            var mockDb = new Mock<IPatronsDatabase>();
            var mockEmail = new Mock<IAmazonSimpleEmailService>();

            // Mock creating an unsubscribe link.
            mockDb.Setup(db => db.CreateMarketingUserUnsubscribeLink(mUser).Result).Returns(expectedUnsubscribeLink);

            // Describe the format of the templated email which is expected to be sent.
            var expEmailSource = "info@patrons.at";
            var expEmailDest = mUser.Email;
            var expEmailTemplate = "marketing-welcome";

            // Create a new emailService.
            var emailService = new EmailService(null, mockDb.Object, mockEmail.Object);

            // Act:
            // Send a marketing welcome email.
            await emailService.SendMarketWelcome(mUser);

            // Assert:
            // Verify that an email has been sent to the correct destination.
            // Verify that the email has the correct source and template name.
            mockEmail.Verify(
                em => em.SendTemplatedEmailAsync(
                    It.Is<SendTemplatedEmailRequest>(
                        email =>
                            email.Source == expEmailSource
                            && email.Destination.ToAddresses.Contains(expEmailDest)
                            && email.Template == expEmailTemplate
                    ),
                    It.IsAny<CancellationToken>()
                )
            );
        }
    }
}
