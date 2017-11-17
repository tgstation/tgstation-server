using System;
using System.Security.Cryptography;
using System.Text;

namespace TGS.Interface
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

		/// <summary>
		/// Gets the <paramref name="owner"/> and <paramref name="name"/> of a given <paramref name="remote"/>. Shows an error if the target remote isn't GitHub
		/// </summary>
		/// <param name="remote">The URL to parse</param>
		/// <param name="owner">The owner of the <paramref name="remote"/> repository</param>
		/// <param name="name">The name of the <paramref name="remote"/> repository</param>
		public static void GetRepositoryRemote(string remote, out string owner, out string name)
		{
			//Assume standard gh format: [(git)|(https)]://github.com/owner/repo(.git)[0-1]
			var splits = remote.Split('/');
			name = splits[splits.Length - 1];
			owner = splits[splits.Length - 2].Split('.')[0];
		}
	}
}
