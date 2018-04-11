using System;
using System.Collections.Generic;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a user on the current <see cref="System.Runtime.InteropServices.OSPlatform"/>
	/// </summary>
	interface ISystemIdentity : IDisposable
	{
		/// <summary>
		/// A unique identifier for the user
		/// </summary>
		string Uid { get; }

		/// <summary>
		/// The user's name
		/// </summary>
		string Username { get; }

		/// <summary>
		/// Groups the user is in
		/// </summary>
		IEnumerable<string> Groups { get; }

		/// <summary>
		/// Clone the <see cref="ISystemIdentity"/> creating another copy that must have <see cref="IDisposable.Dispose"/> called on it
		/// </summary>
		/// <returns>A new <see cref="ISystemIdentity"/> mirroring the current one</returns>
        ISystemIdentity Clone();
    }
}