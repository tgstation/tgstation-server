using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="InstancePermissionSet"/>s
	/// </summary>
	public interface IInstancePermissionSetClient
	{
		/// <summary>
		/// Get the <see cref="InstancePermissionSet"/> associated with the logged on user
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="InstancePermissionSet"/> associated with the logged on user</returns>
		Task<InstancePermissionSet> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSet"/> to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <paramref name="instancePermissionSet"/></returns>
		Task<InstancePermissionSet> GetId(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Get the <see cref="InstancePermissionSet"/>s in the <see cref="Instance"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="InstancePermissionSet"/>s in the instance</returns>
		Task<IReadOnlyList<InstancePermissionSet>> List(CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSet"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task<InstancePermissionSet> Update(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSet"/> to create</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> reulting in the new <see cref="InstancePermissionSet"/></returns>
		Task<InstancePermissionSet> Create(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <paramref name="instancePermissionSet"/>
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSet"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);
	}
}
