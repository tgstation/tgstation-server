using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s
	/// </summary>
	interface IInstanceFactory
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Host.Models.Instance"/></param>
		///	<param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the operation</param>
		/// <returns>A new <see cref="IInstance"/></returns>
		IInstance CreateInstance(Host.Models.Instance metadata, IDatabaseContextFactory databaseContextFactory);
	}
}