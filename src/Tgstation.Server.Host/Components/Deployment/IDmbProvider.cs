using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Provides absolute paths to the latest compiled .dmbs.
	/// </summary>
	public interface IDmbProvider : IAsyncDisposable
	{
		/// <summary>
		/// The file name of the .dmb.
		/// </summary>
		string DmbName { get; }

		/// <summary>
		/// The primary game directory with a trailing directory separator.
		/// </summary>
		string Directory { get; }

		/// <summary>
		/// The <see cref="CompileJob"/> of the .dmb.
		/// </summary>
		Models.CompileJob CompileJob { get; }

		/// <summary>
		/// The <see cref="Api.Models.EngineVersion"/> used to build the .dmb.
		/// </summary>
		EngineVersion EngineVersion { get; }

		/// <summary>
		/// Disposing the <see cref="IDmbProvider"/> won't cause a cleanup of the working directory.
		/// </summary>
		void KeepAlive();
	}
}
