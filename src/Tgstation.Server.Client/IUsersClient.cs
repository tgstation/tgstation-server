using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing <see cref="User"/>s
	/// </summary>
	public interface IUsersClient
	{
		/// <summary>
		/// Read the current user's information and general rights
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns></returns>
		Task<User> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Create a new <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="UserUpdate"/> used to create the new <see cref="User"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The new <see cref="User"/></returns>
		Task<User> Create(UserUpdate user, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="UserUpdate"/> used to update the <see cref="User"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The updated <see cref="User"/></returns>
		Task<User> Update(UserUpdate user, CancellationToken cancellationToken);
	}
}