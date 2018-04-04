using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing server administration
	/// </summary>
	public interface IAdministrationClient : IRightsClient<AdministrationRights>
	{
		/// <summary>
		/// Get the <see cref="Administration"/> represented by the <see cref="IAdministrationClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Administration"/> represented by the <see cref="IAdministrationClient"/></returns>
		Task<Administration> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="Administration"/> setttings
		/// </summary>
		/// <param name="administration">The <see cref="Administration"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(Administration administration, CancellationToken cancellationToken);
	}
}
