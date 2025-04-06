using System;

using Tgstation.Server.Api.Models.Internal;

using YamlDotNet.Serialization;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration for the server swarm system.
	/// </summary>
	public sealed class SwarmConfiguration : SwarmServer
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="SwarmConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Swarm";

		/// <inheritdoc />
		[YamlMember(SerializeAs = typeof(string))]
		public override Uri? Address
		{
			get => base.Address;
			set => base.Address = value;
		}

		/// <inheritdoc />
		[YamlMember(SerializeAs = typeof(string))]
		public override Uri? PublicAddress
		{
			get => base.PublicAddress;
			set => base.PublicAddress = value;
		}

		/// <summary>
		/// The <see cref="SwarmServer.Address"/> of the swarm controller. If <see langword="null"/>, the current server is considered the controller.
		/// </summary>
		[YamlMember(SerializeAs = typeof(string))]
		public Uri? ControllerAddress { get; set; }

		/// <summary>
		/// The private key used for swarm communication.
		/// </summary>
		public string? PrivateKey { get; set; }

		/// <summary>
		/// The number of nodes in addition to the controller required to be connected a server swarm before performing an update.
		/// </summary>
		public uint UpdateRequiredNodeCount { get; set; }

		/// <summary>
		/// The IP address specifier for hosting the swarm service. Defaults to 0.0.0.0.
		/// </summary>
		public string? HostingIP { get; set; }

		/// <summary>
		/// The swarm API port. Defaults to <see cref="GeneralConfiguration.ApiPort"/> + 1.
		/// </summary>
		public ushort? HostingPort { get; set; }
	}
}
