using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Interface for evaluating <see cref="IAuthorizationRequirement"/>s.
	/// </summary>
	public interface IAuthorizationService
	{
		/// <summary>
		/// Attempt to authorize the current context with a given <paramref name="requirement"/>.
		/// </summary>
		/// <param name="requirement">The <see cref="IAuthorizationRequirement"/> to authorize.</param>
		/// <param name="resource">The resource to authorize.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorizationResult"/>.</returns>
		ValueTask<AuthorizationResult> AuthorizeAsync(IEnumerable<IAuthorizationRequirement> requirement, object? resource = null);
	}
}
