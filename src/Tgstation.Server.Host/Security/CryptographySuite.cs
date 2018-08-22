using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class CryptographySuite : ICryptographySuite
	{
		/// <summary>
		/// Generates a secure set of <see cref="byte"/>s
		/// </summary>
		/// <returns>A secure set of <see cref="byte"/>s</returns>
		public static byte[] GetSecureBytes(int amount)
		{
			using (var rng = new RNGCryptoServiceProvider())
			{
				var byt = new byte[amount];
				rng.GetBytes(byt);
				return byt;
			}
		}

		/// <summary>
		/// The <see cref="IPasswordHasher{TUser}"/> for the <see cref="CryptographySuite"/>
		/// </summary>
		readonly IPasswordHasher<User> passwordHasher;

		/// <summary>
		/// Construct a <see cref="CryptographySuite"/>
		/// </summary>
		/// <param name="passwordHasher">The value of <see cref="passwordHasher"/></param>
		public CryptographySuite(IPasswordHasher<User> passwordHasher) => this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

		/// <inheritdoc />
		public void SetUserPassword(User user, string newPassword)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (String.IsNullOrEmpty(newPassword))
				throw new ArgumentNullException(nameof(newPassword));
			user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
			user.LastPasswordUpdate = DateTimeOffset.Now;
		}

		/// <inheritdoc />
		public bool CheckUserPassword(User user, string password)
		{
			switch (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password))
			{
				case PasswordVerificationResult.Failed:
					return false;
				case PasswordVerificationResult.SuccessRehashNeeded:
					user.PasswordHash = passwordHasher.HashPassword(user, password);
					//don't update LastPasswordUpdate since it hasn't actually changed
					break;
			}
			return true;
		}

		/// <inheritdoc />
		public string GetSecureString() => Convert.ToBase64String(GetSecureBytes(30));
	}
}
