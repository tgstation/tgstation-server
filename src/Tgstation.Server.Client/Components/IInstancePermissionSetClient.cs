using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

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
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="InstancePermissionSetResponse"/> associated with the logged on user</returns>
		Task<InstancePermissionSetResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <paramref name="instancePermissionSet"/></returns>
		Task<InstancePermissionSetResponse> GetId(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Get the instance permission sets in the instance.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="InstancePermissionSetResponse"/>s in the instance.</returns>
		Task<IReadOnlyList<InstancePermissionSetResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task<InstancePermissionSetResponse> Update(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> reulting in the new <see cref="InstancePermissionSetResponse"/>.</returns>
		Task<InstancePermissionSetResponse> Create(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSetRequest"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);
	}
}
