using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Implements a fake "dead" <see cref="ISessionController"/>
	/// </summary>
	sealed class DeadSessionController : ISessionController
	{
		/// <inheritdoc />
		public Task<LaunchResult> LaunchResult { get; }

		/// <inheritdoc />
		public bool IsPrimary => false;

		/// <inheritdoc />
		public bool TerminationWasRequested => false;

		/// <inheritdoc />
		public ApiValidationStatus ApiValidationStatus => throw new NotSupportedException();

		/// <inheritdoc />
		public IDmbProvider Dmb { get; }

		/// <inheritdoc />
		public ushort? Port => null;

		/// <inheritdoc />
		public bool ClosePortOnReboot
		{
			get => false;
			set => throw new NotSupportedException();
		}

		/// <inheritdoc />
		public RebootState RebootState => throw new NotSupportedException();

		/// <inheritdoc />
		public Task OnReboot { get; }

		/// <inheritdoc />
		public Task<int> Lifetime { get; }

		/// <summary>
		/// If the <see cref="DeadSessionController"/> was <see cref="Dispose"/>d
		/// </summary>
		bool disposed;

		/// <summary>
		/// Construct a <see cref="DeadSessionController"/>
		/// </summary>
		/// <param name="dmbProvider">The value of <see cref="Dmb"/></param>
		public DeadSessionController(IDmbProvider dmbProvider)
		{
			Dmb = dmbProvider ?? throw new ArgumentNullException(nameof(dmbProvider));
			LaunchResult = Task.FromResult(new LaunchResult
			{
				StartupTime = TimeSpan.FromSeconds(0)
			});
			Lifetime = Task.FromResult(-1);
			OnReboot = new TaskCompletionSource<object>().Task;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (this)
			{
				if (disposed)
					return;
				disposed = true;
			}
			Dmb.Dispose();
		}

		/// <inheritdoc />
		public void EnableCustomChatCommands() => throw new NotSupportedException();

		/// <inheritdoc />
		public ReattachInformation Release() => throw new NotSupportedException();

		/// <inheritdoc />
		public void ResetRebootState() => throw new NotSupportedException();

		/// <inheritdoc />
		public Task<string> SendCommand(string command, CancellationToken cancellationToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public void SetHighPriority() => throw new NotSupportedException();

		/// <inheritdoc />
		public Task<bool> SetPort(ushort newPort, CancellationToken cancellatonToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken) => throw new NotSupportedException();
	}
}
