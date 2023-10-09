using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Temporary <see cref="IDmbProvider"/>.
	/// </summary>
	sealed class TemporaryDmbProvider : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName { get; }

		/// <inheritdoc />
		public string Directory { get; }

		/// <inheritdoc />
		public Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public ByondVersion ByondVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemporaryDmbProvider"/> class.
		/// </summary>
		/// <param name="directory">The value of <see cref="Directory"/>.</param>
		/// <param name="dmb">The value of <see cref="DmbName"/>.</param>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="byondVersion">The value of <see cref="ByondVersion"/>.</param>
		public TemporaryDmbProvider(string directory, string dmb, Models.CompileJob compileJob, ByondVersion byondVersion)
		{
			DmbName = dmb ?? throw new ArgumentNullException(nameof(dmb));
			Directory = directory ?? throw new ArgumentNullException(nameof(directory));
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			ByondVersion = byondVersion ?? throw new ArgumentNullException(nameof(byondVersion));
		}

		/// <inheritdoc />
		public void Dispose()
		{
		}

		/// <inheritdoc />
		public void KeepAlive() => throw new NotSupportedException();
	}
}
