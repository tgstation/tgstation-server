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
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The <see cref="IDmbProvider"/> used by DreamDaemon.
		/// </summary>
		public IDmbProvider Dmb { get; set; }

		/// <summary>
		/// The <see cref="Interop.Bridge.RuntimeInformation"/> for the DMAPI.
		/// </summary>
		public RuntimeInformation RuntimeInformation { get; private set; }

		/// <summary>
		/// The <see cref="TimeSpan"/> which indicates when topic requests should timeout.
		/// </summary>
		public TimeSpan TopicRequestTimeout { get; }

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for accessing <see cref="RuntimeInformation"/>.
		/// </summary>
		readonly object runtimeInformationLock;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class. For use with a given <paramref name="copy"/> and <paramref name="dmb"/>.
		/// </summary>
		/// <param name="copy">The <see cref="Models.ReattachInformation"/> to copy values from.</param>
		/// <param name="dmb">The value of <see cref="Dmb"/>.</param>
		/// <param name="topicRequestTimeout">The value of <see cref="TopicRequestTimeout"/>.</param>
		public ReattachInformation(
			Models.ReattachInformation copy,
			IDmbProvider dmb,
			TimeSpan topicRequestTimeout) : base(copy)
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
		/// <param name="accessIdentifier">The value of <see cref="Interop.DMApiParameters.AccessIdentifier"/>.</param>
		/// <param name="port">The value of <see cref="ReattachInformationBase.Port"/>.</param>
		internal ReattachInformation(
			IDmbProvider dmb,
			IProcess process,
			RuntimeInformation runtimeInformation,
			string accessIdentifier,
			ushort port)
		{
			Dmb = dmb ?? throw new ArgumentNullException(nameof(dmb));
			ProcessId = process?.Id ?? throw new ArgumentNullException(nameof(process));
			RuntimeInformation = runtimeInformation ?? throw new ArgumentNullException(nameof(runtimeInformation));
			if (!runtimeInformation.SecurityLevel.HasValue)
				throw new ArgumentException("runtimeInformation must have a valid SecurityLevel!", nameof(runtimeInformation));

			AccessIdentifier = accessIdentifier ?? throw new ArgumentNullException(nameof(accessIdentifier));

			LaunchSecurityLevel = runtimeInformation.SecurityLevel.Value;
			LaunchVisibility = runtimeInformation.Visibility.Value;
			Port = port;

			runtimeInformationLock = new object();
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
				if (RuntimeInformation != null)
					throw new InvalidOperationException("RuntimeInformation already set!");

				RuntimeInformation = runtimeInformation;
			}
		}
	}
}
