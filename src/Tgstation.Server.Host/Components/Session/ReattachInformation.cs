using System;

using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Parameters necessary for duplicating a <see cref="ISessionController"/> session.
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase, IDisposable
	{
		/// <summary>
		/// The <see cref="IDmbProvider"/> used by DreamDaemon.
		/// </summary>
		public IDmbProvider Dmb { get; set; }

		/// <summary>
		/// The <see cref="Interop.Bridge.RuntimeInformation"/> for the DMAPI.
		/// </summary>
		public RuntimeInformation RuntimeInformation => runtimeInformation ?? throw new InvalidOperationException("RuntimeInformation not set!");

		/// <summary>
		/// The <see cref="TimeSpan"/> which indicates when topic requests should timeout.
		/// </summary>
		public TimeSpan TopicRequestTimeout { get; }

		/// <summary>
		/// If the <see cref="Dmb"/> should be disposed.
		/// </summary>
		public bool DisposeDmb { get; set; } = true;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for accessing <see cref="RuntimeInformation"/>.
		/// </summary>
		readonly object runtimeInformationLock;

		/// <summary>
		/// Backing field for <see cref="RuntimeInformation"/>.
		/// </summary>
		RuntimeInformation? runtimeInformation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class. For use with a given <paramref name="copy"/> and <paramref name="dmb"/>.
		/// </summary>
		/// <param name="copy">The <see cref="Models.ReattachInformation"/> to copy values from.</param>
		/// <param name="dmb">The value of <see cref="Dmb"/>.</param>
		/// <param name="topicRequestTimeout">The value of <see cref="TopicRequestTimeout"/>.</param>
		public ReattachInformation(
			Models.ReattachInformation copy,
			IDmbProvider dmb,
			TimeSpan topicRequestTimeout)
			: base(copy)
		{
			Dmb = dmb ?? throw new ArgumentNullException(nameof(dmb));
			TopicRequestTimeout = topicRequestTimeout;

			runtimeInformationLock = new object();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class.
		/// </summary>
		/// <param name="dmb">The value of <see cref="Dmb"/>.</param>
		/// <param name="process">The <see cref="IProcess"/> used to get the <see cref="ReattachInformationBase.ProcessId"/>.</param>
		/// <param name="runtimeInformation">The value of <see cref="RuntimeInformation"/>.</param>
		/// <param name="topicRequestTimeout">The value of <see cref="TopicRequestTimeout"/>.</param>
		/// <param name="accessIdentifier">The value of <see cref="Interop.DMApiParameters.AccessIdentifier"/>.</param>
		/// <param name="port">The value of <see cref="ReattachInformationBase.Port"/>.</param>
		public ReattachInformation(
			IDmbProvider dmb,
			IProcess process,
			RuntimeInformation runtimeInformation,
			TimeSpan topicRequestTimeout,
			string accessIdentifier,
			ushort port)
			: base(accessIdentifier)
		{
			Dmb = dmb ?? throw new ArgumentNullException(nameof(dmb));
			ProcessId = process?.Id ?? throw new ArgumentNullException(nameof(process));
			this.runtimeInformation = runtimeInformation ?? throw new ArgumentNullException(nameof(runtimeInformation));

			TopicRequestTimeout = topicRequestTimeout;

			LaunchSecurityLevel = runtimeInformation.SecurityLevel;
			LaunchVisibility = runtimeInformation.Visibility;
			Port = port;
			runtimeInformationLock = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (DisposeDmb)
				Dmb.Dispose();
			else
			{
				Dmb.KeepAlive();
				DisposeDmb = true;
			}
		}

		/// <summary>
		/// Set the <see cref="RuntimeInformation"/> post construction.
		/// </summary>
		/// <param name="runtimeInformation">The <see cref="Interop.Bridge.RuntimeInformation"/>.</param>
		public void SetRuntimeInformation(RuntimeInformation runtimeInformation)
		{
			if (runtimeInformation == null)
				throw new ArgumentNullException(nameof(runtimeInformation));

			lock (runtimeInformationLock)
			{
				if (runtimeInformation != null)
					throw new InvalidOperationException("RuntimeInformation already set!");

				this.runtimeInformation = runtimeInformation;
			}
		}
	}
}
