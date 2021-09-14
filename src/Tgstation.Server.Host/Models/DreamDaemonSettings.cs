using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
	{
		/// <summary>
		/// Backing field for <see cref="Instance"/>.
		/// </summary>
		Instance? instance;

		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(instance))]
		public Instance Instance
		{
			get => instance ?? throw new InvalidOperationException("Instance not set!");
			set => instance = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.Port"/>.
		/// </summary>
		public new ushort Port
		{
			get => base.Port ?? throw new InvalidOperationException("Port was null!");
			set => base.Port = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.StartupTimeout"/>.
		/// </summary>
		public new uint StartupTimeout
		{
			get => base.StartupTimeout ?? throw new InvalidOperationException("StartupTimeout was null!");
			set => base.StartupTimeout = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.HeartbeatSeconds"/>.
		/// </summary>
		public new uint HeartbeatSeconds
		{
			get => base.HeartbeatSeconds ?? throw new InvalidOperationException("HeartbeatSeconds was null!");
			set => base.HeartbeatSeconds = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonSettings.AutoStart"/>.
		/// </summary>
		public new bool AutoStart
		{
			get => base.AutoStart ?? throw new InvalidOperationException("AutoStart was null!");
			set => base.AutoStart = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.AllowWebClient"/>.
		/// </summary>
		public new bool AllowWebClient
		{
			get => base.AllowWebClient ?? throw new InvalidOperationException("AllowWebClient was null!");
			set => base.AllowWebClient = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.Visibility"/>.
		/// </summary>
		public new DreamDaemonVisibility Visibility
		{
			get => base.Visibility ?? throw new InvalidOperationException("Visibility was null!");
			set => base.Visibility = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.Visibility"/>.
		/// </summary>
		public new DreamDaemonSecurity SecurityLevel
		{
			get => base.SecurityLevel ?? throw new InvalidOperationException("SecurityLevel was null!");
			set => base.SecurityLevel = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.DreamDaemonLaunchParameters.TopicRequestTimeout"/>.
		/// </summary>
		public new uint TopicRequestTimeout
		{
			get => base.TopicRequestTimeout ?? throw new InvalidOperationException("TopicRequestTimeout was null!");
			set => base.TopicRequestTimeout = value;
		}
	}
}
