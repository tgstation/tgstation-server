using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating and accessing authentication contexts
	/// </summary>
	public interface IAuthenticationContextFactory
	{
		IAuthenticationContext CurrentAuthenticationContext { get; }

		Task CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken);
	}
}
