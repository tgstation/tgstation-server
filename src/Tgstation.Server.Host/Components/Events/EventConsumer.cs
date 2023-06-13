using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Events
{
	/// <inheritdoc />
	sealed class EventConsumer : IEventConsumer
	{
		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="EventConsumer"/>.
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="EventConsumer"/>.
		/// </summary>
		IWatchdog watchdog;

		/// <summary>
		/// Initializes a new instance of the <see cref="EventConsumer"/> class.
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/>.</param>
		public EventConsumer(IConfiguration configuration)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		/// <inheritdoc />
		public async Task HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			if (watchdog == null)
				throw new InvalidOperationException("EventConsumer used without watchdog set!");

			var scriptTask = configuration.HandleEvent(eventType, parameters, cancellationToken);
			await watchdog.HandleEvent(eventType, parameters, cancellationToken);
			await scriptTask;
		}

		/// <summary>
		/// Set the <paramref name="watchdog"/> for the <see cref="EventConsumer"/>.
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		public void SetWatchdog(IWatchdog watchdog)
		{
#pragma warning disable IDE0016 // Use 'throw' expression
			ArgumentNullException.ThrowIfNull(watchdog);
#pragma warning restore IDE0016 // Use 'throw' expression
			if (this.watchdog != null)
				throw new InvalidOperationException("watchdog already set!");
			this.watchdog = watchdog;
		}
	}
}
