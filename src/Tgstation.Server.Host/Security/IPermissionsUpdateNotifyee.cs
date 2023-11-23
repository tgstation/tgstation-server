using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Receives notifications about permissions updates.
	/// </summary>
	public interface IPermissionsUpdateNotifyee
	{
		/// <summary>
		/// Called when a given <paramref name="instancePermissionSet"/> is successfully created.
		/// </summary>
		/// <param name="instancePermissionSet">The <see cref="InstancePermissionSet"/>. <see cref="InstancePermissionSet.PermissionSet"/> must be populated.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask InstancePermissionSetCreated(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Called when an <see cref="InstancePermissionSet"/> is successfully deleted.
		/// </summary>
		/// <param name="permissionSet">The <see cref="PermissionSet"/> of the deleted <see cref="InstancePermissionSet"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask InstancePermissionSetDeleted(PermissionSet permissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Called when a given <see cref="User"/> is successfully disabled.
		/// </summary>
		/// <param name="user">The <see cref="User"/> that was disabled.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UserDisabled(User user, CancellationToken cancellationToken);
	}
}
