using System;
using System.Security.Cryptography;
using System.Text;

namespace TGServiceInterface
{
	/// <summary>
	/// Helper functions used across the server suite
	/// </summary>
	public static class Helpers
	{
		/// <summary>
		/// Takes some <paramref name="cleartext"/> and returns an encrypted version along with the <paramref name="entropy"/> required to decrypt it
		/// </summary>
		/// <param name="cleartext">The <see cref="string"/> to encrypt</param>
		/// <param name="entropy">The entropy required the decrypt the ciphertext</param>
		/// <returns>Ciphertext for the <paramref name="cleartext"/></returns>
		public static string EncryptData(string cleartext, out string entropy)
		{
			// Generate additional entropy (will be used as the Initialization vector)
			byte[] bentropy = new byte[20];
			using (var rng = new RNGCryptoServiceProvider())
				rng.GetBytes(bentropy);

			byte[] ciphertext = ProtectedData.Protect(Encoding.UTF8.GetBytes(cleartext), bentropy, DataProtectionScope.CurrentUser);

			entropy = Convert.ToBase64String(bentropy, 0, bentropy.Length);
			return Convert.ToBase64String(ciphertext, 0, ciphertext.Length);
		}

		/// <summary>
		/// Takes ciphertext and entropy from <see cref="EncryptData(string, out string)"/> and returns the cleartext. Note that this only works if the OS user of the program is the same one that called <see cref="EncryptData(string, out string)"/>
		/// </summary>
		/// <param name="ciphertext">A return value from a previous call to <see cref="EncryptData(string, out string)"/></param>
		/// <param name="entropy">The entropy parameter from the previous call to <see cref="EncryptData(string, out string)"/> that returned <paramref name="ciphertext"/></param>
		/// <returns>The decrypted <see cref="string"/> on sucess or <see langword="null"/> on failure</returns>
		public static string DecryptData(string ciphertext, string entropy)
		{
			try
			{
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), Convert.FromBase64String(entropy), DataProtectionScope.CurrentUser));
			}
			catch
			{
				return null;
			}
		}
	}
}
