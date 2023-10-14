using System;

using Tgstation.Server.Api.Models.Internal;

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
		public override ByondVersion ByondVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemporaryDmbProvider"/> class.
		/// </summary>
		/// <param name="directory">The value of <see cref="Directory"/>.</param>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="byondVersion">The value of <see cref="ByondVersion"/>.</param>
		public TemporaryDmbProvider(string directory, Models.CompileJob compileJob, ByondVersion byondVersion)
		{
			Directory = directory ?? throw new ArgumentNullException(nameof(directory));
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			ByondVersion = byondVersion ?? throw new ArgumentNullException(nameof(byondVersion));
		}

		/// <inheritdoc />
		public override void Dispose()
		{
		}

		/// <inheritdoc />
		public override void KeepAlive() => throw new NotSupportedException();
	}
}
