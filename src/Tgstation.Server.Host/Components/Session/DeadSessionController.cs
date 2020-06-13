using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop.Topic;

namespace Tgstation.Server.Host.Components.Session
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
		public Task OnPrime { get; }

		/// <inheritdoc />
		public Task<int> Lifetime { get; }

		/// <inheritdoc />
		public Version DMApiVersion => throw new NotSupportedException();

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="disposed"/>.
		/// </summary>
		readonly object disposeLock;

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
			OnReboot = Extensions.TaskExtensions.InfiniteTask();
			OnPrime = Extensions.TaskExtensions.InfiniteTask();
			disposeLock = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (disposeLock)
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
		public Task<CombinedTopicResponse> SendCommand(TopicParameters parameters, CancellationToken cancellationToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public void SetHighPriority() => throw new NotSupportedException();

		/// <inheritdoc />
		public Task<bool> SetPort(ushort newPort, CancellationToken cancellatonToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public void ReplaceDmbProvider(IDmbProvider newProvider) => throw new NotSupportedException();

		/// <inheritdoc />
		public void Suspend() => throw new NotSupportedException();

		/// <inheritdoc />
		public void Resume() => throw new NotSupportedException();

		/// <inheritdoc />
		public Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task CreateDump(string outputFile, CancellationToken cancellationToken) => throw new NotSupportedException();
	}
}
