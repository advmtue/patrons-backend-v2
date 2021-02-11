using System;
using Xunit;

using Patrons.CheckIn.API.Services;

namespace Patrons.CheckIn.API.Tests.Services {
    public class PasswordServiceTest
    {
        /// <summary>
        /// Ensure that an ArgumentNullException is thrown if the input password value for CreatePassword
        /// is null or an empty string ("").
        /// </summary>
        /// <param name="value">Invalid null or empty password string</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CreatePassword_ArgumentNullException_NullOrEmptyString(string value)
        {
            // Arrange:
            // Create a new password service.
            var passwordService = new PasswordService();

            // Assert:
            // Ensure that an EmptyPasswordException is thrown.
            Assert.Throws<ArgumentNullException>(() => passwordService.CreatePassword(value));
        }

        /// <summary>
        /// Ensure that the output HashedPassword and Salt are not null in valid calls.
        /// </summary>
        /// <param name="value">Input valid password string</param>
        [Theory]
        [InlineData("abcdefg")]
        [InlineData("a")]
        [InlineData("some-various_password1234567890!@#$%^&*()")]
        public void CreatePassword_Produces_NotNull_Output(string value)
        {
            // Arrange:
            var passwordService = new PasswordService();

            // Act:
            // Create a HashedPasswordWithSalt from the password string.
            HashedPasswordWithSalt result = passwordService.CreatePassword(value);

            // Assert:
            // Ensure that the result is not null.
            Assert.NotNull(result);

            // Ensure that result.HashedPassword is not null.
            Assert.NotNull(result.HashedPassword);

            // Ensure that result.Salt is not null.
            Assert.NotNull(result.Salt);
        }

        /// <summary>
        /// Ensure that an ArgumentNullException is thrown if password or salt are null.
        /// </summary>
        /// <param name="password">Null or valid password</param>
        /// <param name="salt">Null or valid salt</param>
        [Theory]
        // Invalid null password, valid salt.
        [InlineData(null, "validSalt")]
        // Invalid null password, invalid null salt.
        [InlineData(null, null)]
        // Valid password, invalid null salt.
        [InlineData("validPassword", null)]
        public void RegeneratePassword_ArgumentNullException_NullPasswordOrSalt(string password, string salt)
        {
            // Arrange:
            // Create a new password service.
            var passwordService = new PasswordService();

            // Assert:
            // Ensure a ArgumentNullException is thrown.
            Assert.Throws<ArgumentNullException>(() => passwordService.RegeneratePassword(password, salt));
        }

        /// <summary>
        /// Ensure that the regeneration of previously computer values is the same.
        /// </summary>
        /// <param name="password">Clear text password</param>
        /// <param name="salt">Base64 encoded salt</param>
        /// <param name="expected">Base64 encoded hashed password</param>
        [Theory]
        // Password: "password!", Salt: "12345".
        [InlineData(
                "password!",
                "MTIzNDU=",
                "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==")
        ]
        // Password: "1234567890!@#$%^&*()", Salt: "abcdefghijklmnopqrstuvwxyz".
        [InlineData(
                "1234567890!@#$%^&*()",
                "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXo=",
                "pp/1kZTLVr1eejy36ADXn/TssMnQM3ZDeL8mDDWiRKhwWCVF7z7POOWL+vu0AXeKJtju8Cv7HBxl7k+m3dEuhw==")
        ]
        public void RegeneratePassword_Reproducible(string password, string salt, string expected)
        {
            // Arrange:
            // Create a new password service.
            var passwordService = new PasswordService();

            // Act:
            // Regenerate a corresponding HashedPasswordWithSalt.
            HashedPasswordWithSalt result = passwordService.RegeneratePassword(password, salt);

            // Assert:
            // Expect the result to not be null.
            Assert.NotNull(result);
            // Expect the password hash to match the specified expected result.
            Assert.Equal(result.HashedPassword, expected);
            // Expect result.Salt to equal the input salt.
            Assert.Equal(result.Salt, salt);
        }

        [Theory]
        // "MTIzNDU=" == Base64("12345")
        // Null password only.
        [InlineData(null, "MTIzNDU=", "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==")]
        // Null password and salt.
        [InlineData(null, null, "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==")]
        // Null password and hash.
        [InlineData(null, "MTIzNDU=", null)]
        // All values null.
        [InlineData(null, null, null)]
        // Null salt only.
        [InlineData("password!", null, "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==")]
        // Null salt and hash.
        [InlineData("password!", null, null)]
        // Null hash only.
        [InlineData("password!", "MTIzNDU=", null)]
        public void IsPasswordMatch_ArgumentNullException_NullPasswordSaltOrHash(
                string password,
                string salt,
                string hash)
        {
            // Arrange:
            // Create a new password service.
            var passwordService = new PasswordService();

            // Assert:
            // Expect an ArgumentNullException.
            Assert.Throws<ArgumentNullException>(() => passwordService.IsPasswordMatch(password, salt, hash));
        }

        /// <summary>
        /// Test that passwordService.IsPasswordMatch returns correct match results.
        /// </summary>
        /// <param name="pw">Clear-text password</param>
        /// <param name="salt">Base64 encoded salt</param>
        /// <param name="hash">Valid or invalid match for hashed pw + salt combination</param>
        /// <param name="shouldMatch">Should the function return a match?</param>
        [Theory]
        // Valid combination which should match.
        [InlineData(
                "password!",
                "MTIzNDU=", // "12345"
                "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==",
                true)
        ]
        // Modified salt which should not match.
        [InlineData(
                "password!",
                "MTIzNDU2", // "123456"
                "QdWDVNqxVM4pQ4RCoLsTjae4vOuC8EO7JwIPGHxZLmN3oLRBp8YvXRyeBvVCSf9wSJic+Jy45YKvViaWknRJmQ==",
                false)
        ]
        // Modified hash which should not match
        [InlineData(
                "password!",
                "MTIzNDU=", // "12345"
                "thisShouldNotMatchAnythingEver",
                false)
        ]
        public void IsPasswordMatch_TestMatching(string pw, string salt, string hash, bool shouldMatch)
        {
            // Arrange:
            // Create a new password service.
            var passwordService = new PasswordService();

            // Act:
            // Calculate password match.
            bool result = passwordService.IsPasswordMatch(pw, salt, hash);

            // Assert:
            // Ensure the result matches the shouldMatch condition.
            Assert.Equal(result, shouldMatch);
        }
    }
}
