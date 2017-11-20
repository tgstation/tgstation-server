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
		/// Sets <see cref="InstanceConfig.Enabled"/> of <see cref="Config"/> to <see langword="false"/>
		/// </summary>
		void Offline();
	}
}
