using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for managing <see cref="PermissionSet"/>s.
	/// </summary>
	public interface IPermissionSetAuthority : IAuthority
	{
		/// <summary>
		/// Gets the <see cref="User"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> to lookup.</param>
		/// <param name="lookupType">The <see cref="PermissionSetLookupType"/> of <paramref name="id"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="PermissionSet"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		ValueTask<AuthorityResponse<PermissionSet>> GetId(long id, PermissionSetLookupType lookupType, CancellationToken cancellationToken);
	}
}
