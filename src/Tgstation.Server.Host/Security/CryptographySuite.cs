using System;
using System.Globalization;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Identity;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class CryptographySuite : ICryptographySuite
	{
		/// <summary>
		/// Length in <see cref="byte"/>s of generated base64 secure string.
		/// </summary>
		const uint SecureStringLength = 30;

		/// <summary>
		/// The <see cref="IPasswordHasher{TUser}"/> for the <see cref="CryptographySuite"/>.
		/// </summary>
		readonly IPasswordHasher<User> passwordHasher;

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptographySuite"/> class.
		/// </summary>
		/// <param name="passwordHasher">The value of <see cref="passwordHasher"/>.</param>
		public CryptographySuite(IPasswordHasher<User> passwordHasher)
		{
			this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
		}

		/// <inheritdoc />
		public byte[] GetSecureBytes(uint amount)
		{
			using var rng = new RNGCryptoServiceProvider();
			var byt = new byte[amount];
			rng.GetBytes(byt);
			return byt;
		}

		/// <inheritdoc />
		public void SetUserPassword(User user, string newPassword, bool newUser)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (newPassword == null)
				throw new ArgumentNullException(nameof(newPassword));
			user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
			if (!newUser)
				user.LastPasswordUpdate = DateTimeOffset.UtcNow;
		}

		/// <inheritdoc />
		public bool CheckUserPassword(User user, string password)
		{
			var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
			switch (result)
			{
				case PasswordVerificationResult.Failed:
					return false;
				case PasswordVerificationResult.SuccessRehashNeeded:
					SetUserPassword(user, password, false);
					break;
				case PasswordVerificationResult.Success:
					break;
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Password hasher return unknown PasswordVerificationResult: {0}", result));
			}

			return true;
		}

		/// <inheritdoc />
		public string GetSecureString() => Convert.ToBase64String(GetSecureBytes(SecureStringLength));
	}
}
