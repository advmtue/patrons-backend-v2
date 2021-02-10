using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Patrons.CheckIn.API.Services
{
    /// <summary>
    /// A hashed password string with accompanying salt.
    /// </summary>
    public class HashedPasswordWithSalt
    {
        /// <summary>
        /// Hashed password.
        /// </summary>
        public string HashedPassword { get; set; }

        /// <summary>
        /// Salt.
        /// </summary>
        /// <value></value>
        public string Salt { get; set; }
    }

    public class PasswordService
    {
        public PasswordService() { }

        /// <summary>
        /// Run a preconfigured PBKDF2.
        /// </summary>
        /// <param name="password">Clear-text password.</param>
        /// <param name="salt">Salt text.</param>
        /// <returns>Byte array of hashed password.</returns>
        private byte[] _ConfiguredPbkdf2(string password, byte[] salt)
        {
            return KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 20000,
                numBytesRequested: 64
            );
        }

        /// <summary>
        /// Create a new password hash with salt from a clear-text password string.
        /// </summary>
        /// <param name="password">Clear-text password</param>
        /// <returns>Hashed password and salt combination.</returns>
        public HashedPasswordWithSalt CreatePassword(string password)
        {
            // Generate a cryptographically secure salt.
            // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-5.0
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Perform preconfigured hashing using pbkdf2.
            var hashed = this._ConfiguredPbkdf2(password, salt);

            // Convert hash and salt to Base64 strings and combine.
            return new HashedPasswordWithSalt
            {
                HashedPassword = Convert.ToBase64String(hashed),
                Salt = Convert.ToBase64String(salt)
            };
        }

        /// <summary>
        /// Regenerate the hash and salt of a clear-text password + salt combination.
        /// </summary>
        /// <param name="password">Clear-text password</param>
        /// <param name="salt">Password salt to re-use.</param>
        /// <returns></returns>
        public HashedPasswordWithSalt RegeneratePassword(string password, string salt)
        {
            // Convert salt string into bytes.
            byte[] saltBytes = Convert.FromBase64String(salt);

            // Perform preconfigured hashing using pbkdf2.
            var hashed = _ConfiguredPbkdf2(password, saltBytes);

            // Return new hashed password + original salt combination.
            return new HashedPasswordWithSalt
            {
                HashedPassword = Convert.ToBase64String(hashed),
                Salt = salt
            };
        }

        /// <summary>
        /// Check if a clear-text password matches the hashed version with corresponding salt value.
        /// </summary>
        /// <param name="clearTextPassword">Clear-text password</param>
        /// <param name="salt">Salt text</param>
        /// <param name="passwordHash">Hashed password</param>
        /// <returns>True if the clear-text + salt combination is congruent to the hash</returns>
        public bool IsPasswordMatch(string clearTextPassword, string salt, string passwordHash)
        {
            var hashedClearText = RegeneratePassword(clearTextPassword, salt);

            return hashedClearText.HashedPassword == passwordHash;
        }
    }
}
