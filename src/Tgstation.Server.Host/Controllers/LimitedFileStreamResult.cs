using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Very similar to <see cref="FileStreamResult"/> except it's <see cref="IActionResultExecutor{TResult}"/> contains a fix for https://github.com/dotnet/aspnetcore/issues/28189.
	/// </summary>
	public sealed class LimitedFileStreamResult : FileResult
	{
		/// <summary>
		/// The <see cref="global::System.IO.FileStream"/> representing the file to download.
		/// </summary>
		public FileStream FileStream { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LimitedFileStreamResult"/> <see langword="class"/>.
		/// </summary>
		/// <param name="stream">The value of <see cref="FileStream"/>.</param>
		public LimitedFileStreamResult(FileStream stream)
			: base(MediaTypeNames.Application.Octet)
		{
			FileStream = stream ?? throw new ArgumentNullException(nameof(stream));
		}

		/// <inheritdoc />
		public override Task ExecuteResultAsync(ActionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			var executor = context
				.HttpContext
				.RequestServices
				.GetRequiredService<IActionResultExecutor<LimitedFileStreamResult>>();
			return executor.ExecuteAsync(context, this);
		}
	}
}
