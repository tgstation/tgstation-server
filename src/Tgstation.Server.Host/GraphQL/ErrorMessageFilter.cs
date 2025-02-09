using System;

using HotChocolate;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// <see cref="IErrorFilter"/> for transforming <see cref="ErrorMessageResponse"/>-like <see cref="Exception"/>.
	/// </summary>
	sealed class ErrorMessageFilter : IErrorFilter
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ErrorMessageFilter"/>.
		/// </summary>
		readonly ILogger<ErrorMessageFilter> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessageFilter"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ErrorMessageFilter(ILogger<ErrorMessageFilter> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public IError OnError(IError error)
		{
			ArgumentNullException.ThrowIfNull(error);

			if (error.Exception == null)
				return error;

			var errorBuilder = ErrorBuilder.FromError(error)
				.SetException(null)
				.ClearExtensions();

			const string ErrorCodeFieldName = "errorCode";
			const string AdditionalDataFieldName = "additionalData";

			if (error.Exception is DbUpdateException dbUpdateException)
			{
				if (dbUpdateException.InnerException is OperationCanceledException)
				{
					logger.LogTrace(dbUpdateException, "Rethrowing DbUpdateException as OperationCanceledException");
					throw dbUpdateException.InnerException;
				}

				logger.LogDebug(dbUpdateException, "Database conflict!");
				return errorBuilder
					.SetMessage(dbUpdateException.Message)
					.SetExtension(ErrorCodeFieldName, ErrorCode.DatabaseIntegrityConflict)
					.SetExtension(AdditionalDataFieldName, (dbUpdateException.InnerException ?? dbUpdateException).Message)
					.Build();
			}

			if (error.Exception is not ErrorMessageException errorMessageException)
			{
				return errorBuilder
					.SetMessage(error.Exception.Message)
					.SetExtension(ErrorCodeFieldName, ErrorCode.InternalServerError)
					.SetExtension(AdditionalDataFieldName, error.Exception.ToString())
					.Build();
			}

			return errorBuilder
				.SetMessage(errorMessageException.Message)
				.SetExtension(ErrorCodeFieldName, errorMessageException.ErrorCode)
				.SetExtension(AdditionalDataFieldName, errorMessageException.AdditionalData)
				.Build();
		}
	}
}
