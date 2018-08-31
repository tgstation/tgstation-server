using Microsoft.Extensions.Hosting;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s
	/// </summary>
	interface IInstanceFactory : IHostedService
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/></param>
		/// <returns>A new <see cref="IInstance"/></returns>
		IInstance CreateInstance(Models.Instance metadata);
	}
}