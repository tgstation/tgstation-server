using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Provides access to the server's <see cref="HttpApiPort"/>.
	/// </summary>
	interface IServerPortProvider
	{
		/// <summary>
		/// A <see cref="Task{TResult}"/> resulting in the port the server listens on.
		/// </summary>
		Task<ushort> HttpApiPort { get; }

		/// <summary>
		/// Configures the <see cref="ServerPortProivder"/>.
		/// </summary>
		/// <param name="addressFeature">The <see cref="IServerAddressesFeature"/> to use.</param>
		void Configure(IServerAddressesFeature addressFeature);
	}
}
