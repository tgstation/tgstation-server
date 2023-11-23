using Tgstation.Server.Host.Models;

#nullable disable

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Provides the most recently deployed <see cref="CompileJob"/>.
	/// </summary>
	public interface ILatestCompileJobProvider
	{
		/// <summary>
		/// Gets the latest <see cref="CompileJob"/>.
		/// </summary>
		/// <returns>The latest <see cref="CompileJob"/>.</returns>
		CompileJob LatestCompileJob();
	}
}
