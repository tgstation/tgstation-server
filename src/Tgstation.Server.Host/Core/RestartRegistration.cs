using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class RestartRegistration : IRestartRegistration
	{
		/// <summary>
		/// The <see cref="DisposeInvoker"/>.
		/// </summary>
		readonly DisposeInvoker? disposeInvoker;

		/// <summary>
		/// Initializes a new instance of the <see cref="RestartRegistration"/> class.
		/// </summary>
		/// <param name="disposeInvoker">The value of <see cref="disposeInvoker"/>.</param>
		public RestartRegistration(DisposeInvoker? disposeInvoker)
		{
			this.disposeInvoker = disposeInvoker;
		}

		/// <inheritdoc />
		public void Dispose() => disposeInvoker?.Dispose();
	}
}
