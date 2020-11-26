using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace patrons_web_api.Services
{
    public class HashedPasswordWithSalt
    {
        public string HashedPassword { get; set; }
        public string Salt { get; set; }
    }

    public class PasswordService
    {
        public PasswordService() { }

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

        public HashedPasswordWithSalt CreatePassword(string password)
        {
            // Generate salt
            // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-5.0
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Perform preconfigured hashing
            var hashed = this._ConfiguredPbkdf2(password, salt);

            // Convert hash and salt to Base64 strings and combine
            return new HashedPasswordWithSalt
            {
                HashedPassword = Convert.ToBase64String(hashed),
                Salt = Convert.ToBase64String(salt)
            };
        }

        public HashedPasswordWithSalt RegeneratePassword(string password, string salt)
        {
            // Convert salt string into bytes
            byte[] saltBytes = Convert.FromBase64String(salt);

            // Perform preconfigured hashing
            var hashed = _ConfiguredPbkdf2(password, saltBytes);

            return new HashedPasswordWithSalt
            {
                HashedPassword = Convert.ToBase64String(hashed),
                Salt = salt
            };
        }

        public bool IsPasswordMatch(string clearTextPassword, string salt, string passwordHash)
        {
            var hashedClearText = RegeneratePassword(clearTextPassword, salt);

            return hashedClearText.HashedPassword == passwordHash;
        }
    }
}