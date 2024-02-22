using System;

namespace Tgstation.Server.Host.Components.Events
{
	/// <summary>
	/// Attribute for indicating the script that a given <see cref="EventType"/> runs.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	sealed class EventScriptAttribute : Attribute
	{
		/// <summary>
		/// The name and order of the scripts the event script the <see cref="EventType"/> runs.
		/// </summary>
		public string[] ScriptNames { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventScriptAttribute"/> class.
		/// </summary>
		/// <param name="scriptNames">The value of <see cref="ScriptNames"/>.</param>
		public EventScriptAttribute(params string[] scriptNames)
		{
			ScriptNames = scriptNames ?? throw new ArgumentNullException(nameof(scriptNames));
		}
	}
}
