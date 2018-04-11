using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Represents the currently authenticated <see cref="Api.Models.User"/>
	/// </summary>
	interface IAuthenticationContext : IDisposable
	{
		/// <summary>
		/// The <see cref="ISystemIdentity"/> of <see cref="User"/>
		/// </summary>
		ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// The <see cref="Api.Models.User"/> represented by the <see cref="IAuthenticationContext"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="User"/> represented by the <see cref="IAuthenticationContext"/></returns>
		Task<User> User(CancellationToken cancellationToken);

		/// <summary>
		/// The <see cref="Api.Models.InstanceUser"/> represented by <see cref="User"/> and a given <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> of the <see cref="Models.InstanceUser"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Models.InstanceUser"/> represented by <see cref="User"/> and a given <paramref name="instance"/></returns>
		Task<InstanceUser> InstanceUser(Instance instance, CancellationToken cancellationToken);
	}
}