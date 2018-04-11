using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using System.Text;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class CryptographySuite : ICryptographySuite
	{
		/// <summary>
		/// The length of secure strings used in the application
		/// </summary>
		public const int SecureStringLength = 40;

		/// <summary>
		/// Generates a secure ascii <see cref="string"/> of length <see cref="SecureStringLength"/>
		/// </summary>
		/// <returns>A secure ascii <see cref="string"/> of length <see cref="SecureStringLength"/></returns>
		static string GenerateSecureString()
		{
			using (var rng = new RNGCryptoServiceProvider())
			{
				var byt = new byte[1];
				var result = new StringBuilder
				{
					Capacity = SecureStringLength
				};
				while (result.Length < SecureStringLength)
				{
					rng.GetBytes(byt);
					var chr = (char)byt[0];
					if (Char.IsLetterOrDigit(chr))
						result.Append(chr);
				}
				return result.ToString();
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
		}
	}
}
