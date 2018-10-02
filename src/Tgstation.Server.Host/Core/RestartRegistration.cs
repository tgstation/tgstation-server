using System;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class RestartRegistration : IRestartRegistration
	{
		/// <summary>
		/// The <see cref="Dispose"/> <see cref="Action"/>
		/// </summary>
		readonly Action onDispose;

		/// <summary>
		/// Construct a <see cref="RestartRegistration"/>
		/// </summary>
		/// <param name="onDispose">The value of <see cref="onDispose"/></param>
		public RestartRegistration(Action onDispose)
		{
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
		}

		/// <inheritdoc />
		public void Dispose() => onDispose();
	}
}