using System;
using System.Security.Cryptography;
using System.Text;

namespace TGServiceInterface
{
	public static class Helpers
	{
		public static string EncryptData(string data, out string sentropy)
		{
			// Generate additional entropy (will be used as the Initialization vector)
			byte[] entropy = new byte[20];
			using (var rng = new RNGCryptoServiceProvider())
				rng.GetBytes(entropy);

			byte[] ciphertext = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), entropy, DataProtectionScope.CurrentUser);

			sentropy = Convert.ToBase64String(entropy, 0, entropy.Length);
			return Convert.ToBase64String(ciphertext, 0, ciphertext.Length);
		}

		public static string DecryptData(string data, string entropy)
		{
			try
			{
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(data), Convert.FromBase64String(entropy), DataProtectionScope.CurrentUser));
			}
			catch
			{
				return null;
			}
		}
	}
}
