using System;
using TGS.Interface.Components;

namespace TGS.Server
{
	/// <inheritdoc />
	public interface IServer : ITGConnectivity, ITGLanding, ITGInstanceManager, ITGSService, IDisposable
	{
		/// <summary>
		/// Starts the <see cref="IServer"/>
		/// </summary>
		/// <param name="args">The start parameters</param>
		void Start(string[] args);

		/// <summary>
		/// Stops the <see cref="IServer"/>
		/// </summary>
		void Stop();
	}
}
