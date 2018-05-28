using System;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Configures the ASP.NET Core web application
	/// </summary>
	public interface IApplication
	{
		/// <summary>
		/// The version of the <see cref="Application"/>
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The url the server can be reached at locally
		/// </summary>
		string HostingPath { get; }
	}
}