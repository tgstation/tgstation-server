using System;

namespace TGS.Server
{
	/// <summary>
	/// Interface for starting and stopping a server
	/// </summary>
	public interface IServer : IDisposable
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
