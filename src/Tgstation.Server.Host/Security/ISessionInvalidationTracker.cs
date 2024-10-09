using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Handles invalidating user sessions.
	/// </summary>
	public interface ISessionInvalidationTracker
	{
		/// <summary>
		/// Invalidate all sessions for a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The <see cref="User"/> whose sessions should be invalidated.</param>
		public void UserModifiedInvalidateSessions(User user);

		/// <summary>
		/// Track the session represented by a given <paramref name="authenticationContext"/>.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> representing the session to track.</param>
		public void TrackSession(IAuthenticationContext authenticationContext);
	}
}
