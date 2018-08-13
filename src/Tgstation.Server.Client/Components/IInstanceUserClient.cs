using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
    /// <summary>
    /// For managing <see cref="InstanceUser"/>s
    /// </summary>
    public interface IInstanceUserClient
    {
        /// <summary>
        /// Get the <see cref="InstanceUser"/>s in the <see cref="Instance"/>
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
        /// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="InstanceUser"/>s in the instance</returns>
        Task<IReadOnlyList<InstanceUser>> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="instanceUser"/>
		/// </summary>
		/// <param name="instanceUser">The <see cref="InstanceUser"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task<InstanceUser> Update(InstanceUser instanceUser, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="instanceUser"/>
		/// </summary>
		/// <param name="instanceUser">The <see cref="InstanceUser"/> to create</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> reulting in the new <see cref="InstanceUser"/></returns>
		Task<InstanceUser> Create(InstanceUser instanceUser, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <paramref name="instanceUser"/>
		/// </summary>
		/// <param name="instanceUser">The <see cref="InstanceUser"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(InstanceUser instanceUser, CancellationToken cancellationToken);
	}
}