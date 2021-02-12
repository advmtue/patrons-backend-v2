using System;

using Xunit;
using Moq;

using Patrons.CheckIn.API.Services;

namespace Patrons.CheckIn.API.Tests.Services {
    public class RecaptchaServiceTests {

        /// <summary>
        /// Test that passing null values to the construct yields a non-null instance.
        /// </summary>
        [Fact]
        public static void Constructor_Creates()
        {
            // Arrange:
            // Act:
            var recaptchaService = new RecaptchaValidationService(null, null, null);

            // Assert:
            // Ensure the recaptchaService is not null.
            Assert.NotNull(recaptchaService);
        }

        /// <summary>
        /// Test that calling .Validate with a null token results in an ArgumentNullException.
        /// </summary>
        [Fact]
        public static async void Validate_NullToken()
        {
            // Arrange:
            var recaptchaService = new RecaptchaValidationService(null, null, null);

            // Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => recaptchaService.Validate(null)
            );
        }

        /// <summary>
        /// Test that the Validate method returns the correct value for corresponding thresholds.
        /// </summary>
        [Theory]
        [InlineData(0.5, 0.4, false)]
        [InlineData(0.5, 0.6, true)]
        [InlineData(0.5, 0.5, true)]
        public static async void Validate_Threshold(double threshold, double score, bool shouldBe)
        {
            // Arrange:
            // Mock settings.
            string token = "token";

            var mockSettings = new Mock<IRecaptchaValidationSettings>();
            mockSettings.Setup(s => s.ConfidenceThreshold).Returns(threshold);

            // Mock RecaptchaWebService
            var mockWeb = new Mock<IRecaptchaWebService>();

            // Create a RecaptchaResponse which has a score always below the confidenceThreshold.
            RecaptchaResponse response = new RecaptchaResponse {
                Score = score
            };

            mockWeb.Setup(w => w.Get("token").Result).Returns(response);

            // Create a new RecaptchaValidationService.
            var recaptchaService = new RecaptchaValidationService(mockSettings.Object, null, mockWeb.Object);

            // Act:
            bool result = await recaptchaService.Validate(token);

            // Assert that the function returns the correct threshold response.
            Assert.Equal(result, shouldBe);
        }
    }
}
