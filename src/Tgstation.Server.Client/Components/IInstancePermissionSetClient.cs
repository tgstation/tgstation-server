using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing instance permission sets.
	/// </summary>
	public interface IInstancePermissionSetClient
	{
		/// <summary>
		/// Get the instance permission sets associated with the logged on user.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="InstancePermissionSetResponse"/> associated with the logged on user.</returns>
		ValueTask<InstancePermissionSetResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific <paramref name="instancePermissionSet"/>.
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the requested <paramref name="instancePermissionSet"/>.</returns>
		ValueTask<InstancePermissionSetResponse> GetId(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Get the instance permission sets in the instance.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="List{T}"/> of <see cref="InstancePermissionSetResponse"/>s in the instance.</returns>
		ValueTask<List<InstancePermissionSetResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="instancePermissionSet"/>.
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask<InstancePermissionSetResponse> Update(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="instancePermissionSet"/>.
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> reulting in the new <see cref="InstancePermissionSetResponse"/>.</returns>
		ValueTask<InstancePermissionSetResponse> Create(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <paramref name="instancePermissionSet"/>.
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delete(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);
	}
}
