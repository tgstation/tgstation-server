namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Helpers for manipulating the <see cref="Serilog.Context.LogContext"/>.
	/// </summary>
	public static class SerilogContextHelper
	{
		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Models.Instance"/> <see cref="Api.Models.EntityId.Id"/>s.
		/// </summary>
		public const string InstanceIdContextProperty = "Instance";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Models.Job"/> <see cref="Api.Models.EntityId.Id"/>s.
		/// </summary>
		public const string JobIdContextProperty = "Job";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Models.User"/> <see cref="Api.Models.EntityId.Id"/>s.
		/// </summary>
		public const string RequestPathContextProperty = "Request";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Models.Instance"/> <see cref="Api.Models.EntityId.Id"/>s.
		/// </summary>
		public const string UserIdContextProperty = "User";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for the ID of the watchdog monitor iteration currently being processed.
		/// </summary>
		public const string WatchdogMonitorIterationContextProperty = "Monitor";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for the ID of the bridge request currently being processed.
		/// </summary>
		public const string BridgeRequestIterationContextProperty = "Bridge";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for the ID of the chat message currently being processed.
		/// </summary>
		public const string ChatMessageIterationContextProperty = "ChatMessage";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Components.IInstanceReference.Uid"/>s.
		/// </summary>
		public const string InstanceReferenceContextProperty = "InstanceReference";

		/// <summary>
		/// The <see cref="Serilog.Context.LogContext"/> property name for <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s.
		/// </summary>
		public const string SwarmIdentifierContextProperty = "Node";

		/// <summary>
		/// The default value of <see cref="Template"/>.
		/// </summary>
		const string DefaultTemplate = $"Instance:{{{InstanceIdContextProperty}}}|Job:{{{JobIdContextProperty}}}|Request:{{{RequestPathContextProperty}}}|User:{{{UserIdContextProperty}}}|Monitor:{{{WatchdogMonitorIterationContextProperty}}}|Bridge:{{{BridgeRequestIterationContextProperty}}}|Chat:{{{ChatMessageIterationContextProperty}}}|IR:{{{InstanceReferenceContextProperty}}}";

		/// <summary>
		/// Common template used for adding our custom log context to serilog.
		/// </summary>
		/// <remarks>Should not be changed. Only mutable for the sake of identifying swarm nodes under a single test environment</remarks>
		public static string Template { get; private set; }

		/// <summary>
		/// Initializes static members of the <see cref="SerilogContextHelper"/> class.
		/// </summary>
		static SerilogContextHelper()
		{
			Template = DefaultTemplate;
		}

		/// <summary>
		/// Adds the placeholder for the <see cref="SwarmIdentifierContextProperty"/> to the <see cref="Template"/>.
		/// </summary>
		public static void AddSwarmNodeIdentifierToTemplate()
		{
			Template = $"{DefaultTemplate}|Node:{SwarmIdentifierContextProperty}";
		}
	}
}
