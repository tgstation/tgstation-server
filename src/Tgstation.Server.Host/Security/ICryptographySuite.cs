using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Contains various cryptographic functions.
	/// </summary>
	public interface ICryptographySuite
	{
		/// <summary>
		/// Generates a secure set of <see cref="byte"/>s.
		/// </summary>
		/// <param name="amount">The amount of <see cref="byte"/>s to generate.</param>
		/// <returns>A secure set of <see cref="byte"/>s.</returns>
		byte[] GetSecureBytes(uint amount);

		/// <summary>
		/// Sets a <see cref="User.PasswordHash"/> for a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The <see cref="User"/> whos <see cref="User.PasswordHash"/> is to be set.</param>
		/// <param name="newPassword">The new password for the <see cref="User"/>.</param>
		/// <param name="newUser">If the <paramref name="user"/> is just being created.</param>
		void SetUserPassword(User user, string newPassword, bool newUser);

		/// <summary>
		/// Checks a given <paramref name="password"/> matches a given <paramref name="user"/>'s <see cref="User.PasswordHash"/>. This may result in <see cref="User.PasswordHash"/> being modified and this should be persisted.
		/// </summary>
		/// <param name="user">The <see cref="User"/> to check.</param>
		/// <param name="password">The password to check.</param>
		/// <returns><see langword="true"/> if <paramref name="password"/> matches the hash, <see langword="false"/> otherwise.</returns>
		bool CheckUserPassword(User user, string password);

		/// <summary>
		/// Generates a 40-length secure ascii <see cref="string"/>.
		/// </summary>
		/// <returns>A 40-length secure ascii <see cref="string"/>.</returns>
		string GetSecureString();
	}
}
