using System;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// <see cref="Exception"/> thrown when automatic <see cref="IGraphQLServerClient"/> authentication fails.
	/// </summary>
	public sealed class AuthenticationException : Exception
	{
		/// <summary>
		/// The <see cref="ILogin_Login_Errors_ErrorMessageError"/>.
		/// </summary>
		public ILogin_Login_Errors_ErrorMessageError? ErrorMessage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationException"/> class.
		/// </summary>
		/// <param name="errorMessage">The value of <see cref="ErrorMessage"/>.</param>
		public AuthenticationException(ILogin_Login_Errors_ErrorMessageError errorMessage)
			: base(errorMessage?.Message)
		{
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationException"/> class.
		/// </summary>
		public AuthenticationException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationException"/> class.
		/// </summary>
		/// <param name="message">The <see cref="Exception.Message"/>.</param>
		public AuthenticationException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationException"/> class.
		/// </summary>
		/// <param name="message">The <see cref="Exception.Message"/>.</param>
		/// <param name="innerException">The <see cref="Exception.InnerException"/>.</param>
		public AuthenticationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
