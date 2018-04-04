using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an instance of BYOND's DreamDaemon game server. Create action starts the server. Delete action shuts down the server
	/// </summary>
	[Model(typeof(DreamDaemonRights), CanCrud = true)]
	public sealed class DreamDaemon
	{
		/// <summary>
		/// The pull requests merged on the live game version
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public IReadOnlyDictionary<int, string> PullRequests { get; set; }

		/// <summary>
		/// The git sha the live game version was compiled at
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public string Sha { get; set; }

		/// <summary>
		/// The git sha of the origin branch the live game version was compiled at
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public string OriginSha { get; set; }

		/// <summary>
		/// When the live revision was compiled
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public DateTimeOffset CompiledAt { get; set; }

		/// <summary>
		/// If <see cref="DreamDaemon"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetAutoStart)]
		public bool? AutoStart { get; set; }

		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public DreamDaemonStatus? Status { get; set; }

		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetWebClient)]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftRestart)]
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftShutdown)]
		public bool? SoftShutdown { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetSecurity)]
		public DreamDaemonSecurity? SecurirtyLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		public ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		public ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public bool CurrentPort { get; set; }
	}
}
