using System;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

#nullable disable

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Temporary <see cref="IDmbProvider"/>.
	/// </summary>
	sealed class TemporaryDmbProvider : DmbProviderBase
	{
		/// <inheritdoc />
		public override string Directory { get; }

		/// <inheritdoc />
		public override Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public override EngineVersion EngineVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemporaryDmbProvider"/> class.
		/// </summary>
		/// <param name="directory">The value of <see cref="Directory"/>.</param>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="engineVersion">The value of <see cref="EngineVersion"/>.</param>
		public TemporaryDmbProvider(string directory, Models.CompileJob compileJob, EngineVersion engineVersion)
		{
			Directory = directory ?? throw new ArgumentNullException(nameof(directory));
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			EngineVersion = engineVersion ?? throw new ArgumentNullException(nameof(engineVersion));
		}

		/// <inheritdoc />
		public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

		/// <inheritdoc />
		public override void KeepAlive() => throw new NotSupportedException();
	}
}
