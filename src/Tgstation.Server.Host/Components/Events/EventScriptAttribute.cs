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
		/// The name of the script the event script the <see cref="EventType"/> runs.
		/// </summary>
		public string ScriptName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventScriptAttribute"/> <see langword="class"/>.
		/// </summary>
		/// <param name="scriptName">The value of <see cref="ScriptName"/>.</param>
		public EventScriptAttribute(string scriptName)
		{
			ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
		}
	}
}
