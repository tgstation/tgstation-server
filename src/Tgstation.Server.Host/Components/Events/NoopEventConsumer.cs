using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Events
{
	/// <summary>
	/// No-op implementation of <see cref="IEventConsumer"/>.
	/// </summary>
	sealed class NoopEventConsumer : IEventConsumer
	{
		/// <inheritdoc />
		public ValueTask HandleEvent(EventType eventType, IEnumerable<string?> parameters, bool deploymentPipeline, CancellationToken cancellationToken)
			=> ValueTask.CompletedTask;
	}
}
