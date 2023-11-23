using Microsoft.Extensions.Logging;

#nullable disable

namespace Tgstation.Server.Host.Setup
{
	/// <summary>
	/// <see cref="IPostSetupServices"/> with a <see cref="Logger"/>.
	/// </summary>
	/// <typeparam name="TLoggerType">The category <see cref="global::System.Type"/> for <see cref="Logger"/>.</typeparam>
	interface IPostSetupServices<TLoggerType> : IPostSetupServices
	{
		/// <summary>
		/// The <see cref="ILogger"/>.
		/// </summary>
		ILogger<TLoggerType> Logger { get; }
	}
}
