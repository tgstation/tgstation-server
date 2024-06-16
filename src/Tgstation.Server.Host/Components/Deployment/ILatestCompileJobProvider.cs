using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the latest <see cref="CompileJob"/> or <see langword="null"/> if none are available.</returns>
		ValueTask<CompileJob?> LatestCompileJob();
	}
}
