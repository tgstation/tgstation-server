using System;
using System.Collections.Generic;

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
		public IReadOnlyList<string> ScriptNames { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventScriptAttribute"/> class.
		/// </summary>
		/// <param name="scriptNames">The value of <see cref="ScriptNames"/>.</param>
		public EventScriptAttribute(params string[] scriptNames)
		{
			ScriptNames = ScriptNames ?? throw new ArgumentNullException(nameof(scriptNames));
		}
	}
}
