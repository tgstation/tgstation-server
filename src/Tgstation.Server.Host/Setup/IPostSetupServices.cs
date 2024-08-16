using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Setup
{
	/// <summary>
	/// Set of objects needed to configure an <see cref="Core.Application"/>.
	/// </summary>
	public interface IPostSetupServices
	{
		/// <summary>
		/// The <see cref="Configuration.GeneralConfiguration"/>.
		/// </summary>
		GeneralConfiguration GeneralConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.DatabaseConfiguration"/>.
		/// </summary>
		DatabaseConfiguration DatabaseConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.SecurityConfiguration"/>.
		/// </summary>
		SecurityConfiguration SecurityConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.FileLoggingConfiguration"/>.
		/// </summary>
		FileLoggingConfiguration FileLoggingConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.ElasticsearchConfiguration"/>.
		/// </summary>
		ElasticsearchConfiguration ElasticsearchConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.InternalConfiguration"/>.
		/// </summary>
		InternalConfiguration InternalConfiguration { get; }

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/>.
		/// </summary>
		IPlatformIdentifier PlatformIdentifier { get; }

		/// <summary>
		/// If an application reload to get updated configuration values is required.
		/// </summary>
		bool ReloadRequired { get; set; }
	}
}
