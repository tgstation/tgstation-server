using System;

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
			ByondVersion.Engine.Value switch
			{
				EngineType.Byond => ".dmb",
				EngineType.OpenDream => ".json",
				_ => throw new InvalidOperationException($"Invalid EngineType: {ByondVersion.Engine.Value}"),
			});

		/// <inheritdoc />
		public abstract string Directory { get; }

		/// <inheritdoc />
		public abstract Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public abstract ByondVersion ByondVersion { get; }

		/// <inheritdoc />
		public abstract void Dispose();

		/// <inheritdoc />
		public abstract void KeepAlive();
	}
}
