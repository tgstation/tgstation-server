using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Properties;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Represents the header that must be present for every server request.
	/// </summary>
	public sealed class ApiHeaders
	{
		/// <summary>
		/// The <see cref="ApiVersion"/> header key.
		/// </summary>
		public const string ApiVersionHeader = "Api";

		/// <summary>
		/// The <see cref="InstanceId"/> header key.
		/// </summary>
		public const string InstanceIdHeader = "Instance";

		/// <summary>
		/// The <see cref="OAuthProvider"/> header key.
		/// </summary>
		public const string OAuthProviderHeader = "OAuthProvider";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string BearerAuthenticationScheme = "Bearer";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string BasicAuthenticationScheme = "Basic";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string OAuthAuthenticationScheme = "OAuth";

		/// <summary>
		/// Added to <see cref="MediaTypeNames.Application"/> in netstandard2.1. Can't use because of lack of .NET Framework support.
		/// </summary>
		public const string ApplicationJsonMime = "application/json";

		/// <summary>
		/// Added to <see cref="MediaTypeNames.Application"/> in netstandard2.1. Can't use because of lack of .NET Framework support.
		/// </summary>
		const string TextEventStreamMime = "text/event-stream";

		/// <summary>
		/// Get the version of the <see cref="Api"/> the caller is using.
		/// </summary>
		public static readonly Version Version = Version.Parse(ApiVersionAttribute.Instance.RawApiVersion);

		/// <summary>
		/// The current <see cref="System.Reflection.AssemblyName"/>.
		/// </summary>
		static readonly AssemblyName AssemblyName = Assembly.GetExecutingAssembly().GetName();

		/// <summary>
		/// A <see cref="char"/> <see cref="Array"/> containing the ':' <see cref="char"/>.
		/// </summary>
		static readonly char[] ColonSeparator = [':'];

		/// <summary>
		/// The instance <see cref="EntityId.Id"/> being accessed.
		/// </summary>
		public long? InstanceId { get; set; }

		/// <summary>
		/// The client's user agent as a <see cref="ProductHeaderValue"/> if valid.
		/// </summary>
		public ProductHeaderValue? UserAgent => ProductInfoHeaderValue.TryParse(RawUserAgent, out var userAgent) ? userAgent.Product : null;

		/// <summary>
		/// The client's raw user agent.
		/// </summary>
		public string? RawUserAgent { get; }

		/// <summary>
		/// The client's API version.
		/// </summary>
		public Version ApiVersion { get; }

		/// <summary>
		/// The client's <see cref="TokenResponse"/>.
		/// </summary>
		public TokenResponse? Token { get; }

		/// <summary>
		/// The client's username.
		/// </summary>
		public string? Username { get; }

		/// <summary>
		/// The client's password.
		/// </summary>
		public string? Password { get; }

		/// <summary>
		/// The OAuth code in use.
		/// </summary>
		public string? OAuthCode { get; }

		/// <summary>
		/// The <see cref="Models.OAuthProvider"/> the <see cref="Token"/> is for, if any.
		/// </summary>
		public OAuthProvider? OAuthProvider { get; }

		/// <summary>
		/// If the header uses OAuth or TGS JWT authentication.
		/// </summary>
		public bool IsTokenAuthentication => Token != null && !OAuthProvider.HasValue;

		/// <summary>
		/// Checks if a given <paramref name="otherVersion"/> is compatible with our own.
		/// </summary>
		/// <param name="otherVersion">The <see cref="Version"/> to test.</param>
		/// <returns><see langword="true"/> if the given version is compatible with the API. <see langword="false"/> otherwise.</returns>
		public static bool CheckCompatibility(Version otherVersion) => Version.Major == (otherVersion?.Major ?? throw new ArgumentNullException(nameof(otherVersion)));

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class. Used for token authentication.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="token">The value of <see cref="Token"/>.</param>
		public ApiHeaders(ProductHeaderValue userAgent, TokenResponse token)
			: this(userAgent, token, null, null)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			if (token.Bearer == null)
				throw new InvalidOperationException("token.Bearer must be set!");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class. Used for token authentication.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="oAuthCode">The value of <see cref="OAuthCode"/>.</param>
		/// <param name="oAuthProvider">The value of <see cref="OAuthProvider"/>.</param>
		public ApiHeaders(ProductHeaderValue userAgent, string oAuthCode, OAuthProvider oAuthProvider)
			: this(userAgent, null, null, null)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));

			OAuthCode = oAuthCode ?? throw new ArgumentNullException(nameof(oAuthCode));
			OAuthProvider = oAuthProvider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class. Used for password authentication.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="username">The value of <see cref="Username"/>.</param>
		/// <param name="password">The value of <see cref="Password"/>.</param>
		public ApiHeaders(ProductHeaderValue userAgent, string username, string password)
			: this(userAgent, null, username, password)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class.
		/// </summary>
		/// <param name="requestHeaders">The <see cref="RequestHeaders"/> containing the serialized <see cref="ApiHeaders"/>.</param>
		/// <param name="ignoreMissingAuth">If a missing <see cref="HeaderNames.Authorization"/> should be ignored.</param>
		/// <param name="allowEventStreamAccept">If <see cref="TextEventStreamMime"/> is a valid accept.</param>
		/// <exception cref="HeadersException">Thrown if the <paramref name="requestHeaders"/> constitue invalid <see cref="ApiHeaders"/>.</exception>
#pragma warning disable CA1502 // TODO: Decomplexify
		public ApiHeaders(RequestHeaders requestHeaders, bool ignoreMissingAuth, bool allowEventStreamAccept)
		{
			if (requestHeaders == null)
				throw new ArgumentNullException(nameof(requestHeaders));

			var badHeaders = HeaderErrorTypes.None;
			var errorBuilder = new StringBuilder();
			var multipleErrors = false;
			void AddError(HeaderErrorTypes headerType, string message)
			{
				if (badHeaders != HeaderErrorTypes.None)
				{
					multipleErrors = true;
					errorBuilder.AppendLine();
				}

				badHeaders |= headerType;
				errorBuilder.Append(message);
			}

			var jsonAccept = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(ApplicationJsonMime);
			var eventStreamAccept = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(TextEventStreamMime);
			if (!requestHeaders.Accept.Any(accept => accept.IsSubsetOf(jsonAccept)))
				if (!allowEventStreamAccept)
					AddError(HeaderErrorTypes.Accept, $"Client does not accept {ApplicationJsonMime}!");
				else if (!requestHeaders.Accept.Any(eventStreamAccept.IsSubsetOf))
					AddError(HeaderErrorTypes.Accept, $"Client does not accept {ApplicationJsonMime} or {TextEventStreamMime}!");

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) || userAgentValues.Count == 0)
				AddError(HeaderErrorTypes.UserAgent, $"Missing {HeaderNames.UserAgent} header!");
			else
			{
				RawUserAgent = userAgentValues.First();
				if (String.IsNullOrWhiteSpace(RawUserAgent))
					AddError(HeaderErrorTypes.UserAgent, $"Malformed {HeaderNames.UserAgent} header!");
			}

			// make sure the api header matches ours
			Version? apiVersion = null;
			if (!requestHeaders.Headers.TryGetValue(ApiVersionHeader, out var apiUserAgentHeaderValues) || !ProductInfoHeaderValue.TryParse(apiUserAgentHeaderValues.FirstOrDefault(), out var apiUserAgent) || apiUserAgent.Product.Name != AssemblyName.Name)
				AddError(HeaderErrorTypes.Api, $"Missing {ApiVersionHeader} header!");
			else if (!Version.TryParse(apiUserAgent.Product.Version, out apiVersion))
				AddError(HeaderErrorTypes.Api, $"Malformed {ApiVersionHeader} header!");

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorization))
			{
				if (!ignoreMissingAuth)
					AddError(HeaderErrorTypes.AuthorizationMissing, $"Missing {HeaderNames.Authorization} header!");
			}
			else
			{
				var auth = authorization.First();
				var splits = new List<string>(auth?.Split(' ') ?? Enumerable.Empty<string>());
				var scheme = splits.First();
				if (String.IsNullOrWhiteSpace(scheme))
					AddError(HeaderErrorTypes.AuthorizationInvalid, "Missing authentication scheme!");
				else
				{
					splits.RemoveAt(0);
					var parameter = String.Concat(splits);
					if (String.IsNullOrEmpty(parameter))
						AddError(HeaderErrorTypes.AuthorizationInvalid, "Missing authentication parameter!");
					else
					{
						if (requestHeaders.Headers.TryGetValue(InstanceIdHeader, out var instanceIdValues))
						{
							var instanceIdString = instanceIdValues.FirstOrDefault();
							if (instanceIdString != default && Int64.TryParse(instanceIdString, out var instanceId))
								InstanceId = instanceId;
						}

						switch (scheme)
						{
							case OAuthAuthenticationScheme:
								if (requestHeaders.Headers.TryGetValue(OAuthProviderHeader, out StringValues oauthProviderValues))
								{
									var oauthProviderString = oauthProviderValues.First();
									if (Enum.TryParse<OAuthProvider>(oauthProviderString, true, out var oauthProvider))
										OAuthProvider = oauthProvider;
									else
										AddError(HeaderErrorTypes.OAuthProvider, "Invalid OAuth provider!");
								}
								else
									AddError(HeaderErrorTypes.OAuthProvider, $"Missing {OAuthProviderHeader} header!");

								OAuthCode = parameter;
								break;
							case BearerAuthenticationScheme:
								Token = new TokenResponse
								{
									Bearer = parameter,
								};

								try
								{
									Token.ParseJwt();
								}
								catch (ArgumentException ex) when (ex is not ArgumentNullException)
								{
									AddError(HeaderErrorTypes.AuthorizationInvalid, $"Invalid JWT: {ex.Message}");
								}

								break;
							case BasicAuthenticationScheme:
								string badBasicAuthHeaderMessage = $"Invalid basic {HeaderNames.Authorization} header!";
								string joinedString;
								try
								{
									var base64Bytes = Convert.FromBase64String(parameter);
									joinedString = Encoding.UTF8.GetString(base64Bytes);
								}
								catch
								{
									AddError(HeaderErrorTypes.AuthorizationInvalid, badBasicAuthHeaderMessage);
									break;
								}

								var basicAuthSplits = joinedString.Split(ColonSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
								if (basicAuthSplits.Length < 2)
								{
									AddError(HeaderErrorTypes.AuthorizationInvalid, badBasicAuthHeaderMessage);
									break;
								}

								Username = basicAuthSplits.First();
								Password = String.Concat(basicAuthSplits.Skip(1));
								break;
							default:
								AddError(HeaderErrorTypes.AuthorizationInvalid, "Invalid authentication scheme!");
								break;
						}
					}
				}
			}

			if (badHeaders != HeaderErrorTypes.None)
			{
				if (multipleErrors)
					errorBuilder.Insert(0, $"Multiple header validation errors occurred:{Environment.NewLine}");

				throw new HeadersException(badHeaders, errorBuilder.ToString());
			}

			ApiVersion = apiVersion!.Semver();
		}
#pragma warning restore CA1502

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="token">The value of <see cref="Token"/>.</param>
		/// <param name="username">The value of <see cref="Username"/>.</param>
		/// <param name="password">The value of <see cref="Password"/>.</param>
		ApiHeaders(ProductHeaderValue userAgent, TokenResponse? token, string? username, string? password)
		{
			RawUserAgent = userAgent?.ToString();
			Token = token;
			Username = username;
			Password = password;
			ApiVersion = Version;
		}

		/// <summary>
		/// Checks if the <see cref="ApiVersion"/> is compatible with <see cref="Version"/>.
		/// </summary>
		/// <param name="alternateApiVersion">The <see cref="System.Version"/> that can alternatively be used as the <see cref="ApiVersion"/>.</param>
		/// <returns><see langword="true"/> if the API is compatible, <see langword="false"/> otherwise.</returns>
		public bool Compatible(Version? alternateApiVersion = null) => CheckCompatibility(ApiVersion) || (alternateApiVersion != null && alternateApiVersion.Major == ApiVersion.Major);

		/// <summary>
		/// Set <see cref="HttpRequestHeaders"/> using the <see cref="ApiHeaders"/>. This initially clears <paramref name="headers"/>.
		/// </summary>
		/// <param name="headers">The <see cref="HttpRequestHeaders"/> to set.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> for the request.</param>
		public void SetRequestHeaders(HttpRequestHeaders headers, long? instanceId = null)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));
			if (instanceId.HasValue && InstanceId.HasValue && instanceId != InstanceId)
				throw new InvalidOperationException("Specified different instance IDs in constructor and SetRequestHeaders!");

			headers.Clear();
			headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJsonMime));
			headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
			headers.Add(ApiVersionHeader, CreateApiVersionHeader());
			if (OAuthProvider.HasValue)
			{
				headers.Authorization = new AuthenticationHeaderValue(OAuthAuthenticationScheme, OAuthCode!);
				headers.Add(OAuthProviderHeader, OAuthProvider.ToString());
			}
			else if (!IsTokenAuthentication)
				headers.Authorization = new AuthenticationHeaderValue(
					BasicAuthenticationScheme,
					Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));
			else
				headers.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, Token!.Bearer);

			instanceId ??= InstanceId;
			if (instanceId.HasValue)
				headers.Add(InstanceIdHeader, instanceId.Value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Adds the <paramref name="headers"/> necessary for a SignalR hub connection.
		/// </summary>
		/// <param name="headers">The headers <see cref="IDictionary{TKey, TValue}"/> to write to.</param>
		public void SetHubConnectionHeaders(IDictionary<string, string> headers)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			headers.Add(HeaderNames.UserAgent, RawUserAgent ?? throw new InvalidOperationException("Missing UserAgent!"));
			headers.Add(HeaderNames.Accept, ApplicationJsonMime);
			headers.Add(ApiVersionHeader, CreateApiVersionHeader());
		}

		/// <summary>
		/// Create the <see cref="string"/>ified for of the <see cref="ApiVersionHeader"/>.
		/// </summary>
		/// <returns>A <see cref="string"/> representing the <see cref="ApiVersion"/>.</returns>
		string CreateApiVersionHeader()
			=> new ProductHeaderValue(AssemblyName.Name, ApiVersion.ToString()).ToString();
	}
}
