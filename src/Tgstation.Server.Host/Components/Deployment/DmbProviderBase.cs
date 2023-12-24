using System;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	abstract class DmbProviderBase : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName => String.Concat(
			CompileJob.DmeName,
			EngineVersion.Engine switch
			{
				EngineType.Byond => ".dmb",
				EngineType.OpenDream => ".json",
				_ => throw new InvalidOperationException($"Invalid EngineType: {EngineVersion.Engine}"),
			});

		/// <inheritdoc />
		public abstract string Directory { get; }

		/// <inheritdoc />
		public abstract Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public abstract EngineVersion EngineVersion { get; }

		/// <inheritdoc />
		public abstract ValueTask DisposeAsync();

		/// <inheritdoc />
		public abstract void KeepAlive();
	}
}
