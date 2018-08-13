using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the <see cref="Byond"/> installation
	/// </summary>
	public interface IByondClient
	{
		/// <summary>
		/// Get the <see cref="Byond"/> information
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Byond"/> information</returns>
		Task<Byond> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="version">The <see cref="Byond"/> information to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Byond"/> information</returns>
		Task<Byond> Update(Byond byond, CancellationToken cancellationToken);
	}
}
