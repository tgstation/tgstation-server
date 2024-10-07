using System;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using StrawberryShake;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// Extensions for <see cref="ILoginResult"/>.
	/// </summary>
	static class LoginResultExtensions
	{
		/// <summary>
		/// Check a given <paramref name="loginResult"/> for errors.
		/// </summary>
		/// <param name="loginResult">The <see cref="IOperationResult{TResultData}"/> containing the <see cref="ILoginResult"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <returns>The <see cref="JsonWebToken"/> from the successful <paramref name="loginResult"/>.</returns>
		/// <exception cref="AuthenticationException">Thrown when the <paramref name="loginResult"/> is errored.</exception>
		public static JsonWebToken EnsureSuccess(this IOperationResult<ILoginResult> loginResult, ILogger logger)
		{
			ArgumentNullException.ThrowIfNull(loginResult);

			try
			{
				loginResult.EnsureNoErrors();
			}
			catch (GraphQLClientException ex)
			{
				throw new AuthenticationException("Login attempt errored at the GraphQL level!", ex);
			}

			var data = loginResult.Data!.Login;
			var errors = data.Errors;
			if (errors != null)
			{
				foreach (var error in errors)
				{
					if (error is ILogin_Login_Errors_ErrorMessageError errorMessageError)
						logger.LogError(
							 "Authentication error ({code}): {message}{additionalData}",
							 errorMessageError.ErrorCode?.ToString() ?? "No Code",
							 errorMessageError.Message,
							 errorMessageError.AdditionalData != null
								? $"{Environment.NewLine}{errorMessageError.AdditionalData}"
								: String.Empty);
					else
						logger.LogError(
							"Unknown authentication error: {error}",
							error);
				}
			}

			if (data.LoginResult == null)
			{
				if (errors != null)
				{
					var errorMessage = errors.OfType<ILogin_Login_Errors_ErrorMessageError>().FirstOrDefault();
					if (errorMessage != null)
						throw new AuthenticationException(errorMessage);

					throw new AuthenticationException($"Null bearer field and {errors.Count} non-ErrorMessage errors:{(errors.Count > 0 ? $"{Environment.NewLine}\t- {String.Join($"{Environment.NewLine}\t- ", errors)}" : String.Empty)}");
				}

				throw new AuthenticationException($"Null bearer and error fields!");
			}

			return data.LoginResult.Bearer;
		}
	}
}
