using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Controller version of <see cref="IInstanceCore"/>.
	/// </summary>
	public interface IInstanceReference : IInstanceCore, IDisposable
	{
		/// <summary>
		/// A unique ID for the <see cref="IInstanceReference"/>.
		/// </summary>
		public ulong Uid { get; }
	}
}
