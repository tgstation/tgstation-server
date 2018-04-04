using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the compiler
	/// </summary>
	public interface IDreamMakerClient : IRightsClient<DreamMakerRights>
	{
		/// <summary>
		/// Get the <see cref="DreamMaker"/> represented by the <see cref="IDreamMakerClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DreamMaker"/> represented by the <see cref="IDreamMakerClient"/></returns>
		Task<Chat> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="DreamMaker"/> setttings
		/// </summary>
		/// <param name="dreamMaker">The <see cref="DreamMaker"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(DreamMaker dreamMaker, CancellationToken cancellationToken);

		/// <summary>
		/// Compile the current repository revision
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Compile(CancellationToken cancellationToken);
	}
}
