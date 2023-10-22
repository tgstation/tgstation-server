using System;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

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
		public CompileJob CompileJob { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemporaryDmbProvider"/> class.
		/// </summary>
		/// <param name="directory">The value of <see cref="Directory"/>.</param>
		/// <param name="dmb">The value of <see cref="DmbName"/>.</param>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		public TemporaryDmbProvider(string directory, string dmb, CompileJob compileJob)
		{
			DmbName = dmb ?? throw new ArgumentNullException(nameof(dmb));
			Directory = directory ?? throw new ArgumentNullException(nameof(directory));
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;

		/// <inheritdoc />
		public void KeepAlive() => throw new NotSupportedException();
	}
}
