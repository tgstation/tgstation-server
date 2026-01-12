using System;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions methods for <see cref="IInstanceManager"/>.
	/// </summary>
	static class InstanceManagerExtensions
	{
		/// <summary>
		/// Get the <see cref="IInstanceReference"/> associated with given <paramref name="metadata"/>.
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> to get the <see cref="IInstance"/> from.</param>
		/// <param name="metadata">The <see cref="Models.Instance"/> of the desired <see cref="IInstance"/>.</param>
		/// <returns>The <see cref="IInstance"/> associated with the given <paramref name="metadata"/> if it is online, <see langword="null"/> otherwise.</returns>
		public static IInstanceReference? GetInstanceReference(this IInstanceManager instanceManager, Api.Models.Instance metadata)
		{
			ArgumentNullException.ThrowIfNull(instanceManager);
			return instanceManager.GetInstanceReference(metadata.Require(x => x.Id));
		}
	}
}
