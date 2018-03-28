using System;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IInstance : ITGInstance, IDisposable
	{
		/// <summary>
		/// Set the <see cref="IInstance"/> to reattach to a DreamDaemon process when it's disposed
		/// </summary>
		/// <param name="silent">If <see langword="true"/>, suppresses the resulting chat message</param>
		void Reattach(bool silent);

		/// <summary>
		/// Disables the <see cref="IInstance"/>
		/// </summary>
		void Offline();
	}
}
