using System;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Very similar to <see cref="FileStreamResult"/> except it's <see cref="IActionResultExecutor{TResult}"/> contains a fix for https://github.com/dotnet/aspnetcore/issues/28189.
	/// </summary>
	public sealed class LimitedStreamResult : FileResult, IFileStreamProvider
	{
		/// <summary>
		/// The <see cref="Stream"/> result.
		/// </summary>
		readonly Stream stream;

		/// <summary>
		/// Initializes a new instance of the <see cref="LimitedStreamResult"/> class.
		/// </summary>
		/// <param name="stream">The value of <see cref="Stream"/>.</param>
		public LimitedStreamResult(Stream stream)
			: base(MediaTypeNames.Application.Octet)
		{
			this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => stream.DisposeAsync();

		/// <inheritdoc />
		public override Task ExecuteResultAsync(ActionContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			var executor = context
				.HttpContext
				.RequestServices
				.GetRequiredService<IActionResultExecutor<LimitedStreamResult>>();
			return executor.ExecuteAsync(context, this);
		}

		/// <inheritdoc />
		public Task<Stream> GetResult(CancellationToken cancellationToken) => Task.FromResult(stream);
	}
}
